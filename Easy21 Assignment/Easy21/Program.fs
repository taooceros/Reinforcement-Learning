open System
open System.Collections.Generic
open Plotly.NET
open Plotly.NET.LayoutObjects
open XPlot.Plotly

[<Struct>]
type State = { dealer_display: int; player_sum: int }

type Action =
    | Hit = 0
    | Stick = 1

let step s a =
    match a with
    | Action.Hit ->
        let newCard = Random.Shared.Next(1, 11)
        let red = Random.Shared.NextDouble() < 1.0 / 3.0

        let sum = s.player_sum + if red then -newCard else newCard

        if sum > 21 || sum < 1 then
            ({ dealer_display = s.dealer_display; player_sum = sum }, double -1)
        else
            ({ dealer_display = s.dealer_display; player_sum = sum }, double 0)
    | Action.Stick ->
        let mutable dealer_sum = s.dealer_display

        while dealer_sum < 17 do
            let newCard = Random.Shared.Next(1, 11)
            let red = Random.Shared.NextDouble() < 1.0 / 3.0

            dealer_sum <- dealer_sum + if red then -newCard else newCard

        //        printfn $"Dealer Sum: %d{dealer_sum}\nPlayer Sum: %d{s.player_sum}"

        if dealer_sum > 21 then
            ({ dealer_display = dealer_sum; player_sum = s.player_sum }, double 1)
        else if dealer_sum > s.player_sum then
            ({ dealer_display = dealer_sum; player_sum = s.player_sum }, double -1)
        else if dealer_sum < s.player_sum then
            ({ dealer_display = dealer_sum; player_sum = s.player_sum }, double 1)
        else
            ({ dealer_display = dealer_sum; player_sum = s.player_sum }, double 0)
    | _ -> raise (ArgumentException("Invalid action"))

let isTerminal s =
    s.player_sum > 21
    || s.player_sum < 1
    || s.dealer_display > 21
    || s.dealer_display >= 17

let monte_carlo_training =
    lazy
        let value_map = Dictionary<Tuple<State, Action>, double>()
        let num_visits_pair = Dictionary<Tuple<State, Action>, int>()
        let num_visits = Dictionary<State, int>()
        let N0 = 100.0

        for _ in 1..150000 do
            let mutable s =
                { dealer_display = Random.Shared.Next(1, 11)
                  player_sum = Random.Shared.Next(1, 11) }

            let mutable r = 0.0
            let visits = List<Tuple<State, Action>>()

            while not (isTerminal s) do
                let epsilon = N0 / (N0 + (double (num_visits.GetValueOrDefault(s, 0))))

                num_visits[s] <- num_visits.GetValueOrDefault(s, 0) + 1

                let a =
                    if Random.Shared.NextDouble() < epsilon then
                        enum<Action> (Random.Shared.Next(0, 2))
                    else if
                        value_map.GetValueOrDefault((s, Action.Hit), 0)
                        >= value_map.GetValueOrDefault((s, Action.Stick), 0)
                    then
                        Action.Hit
                    else
                        Action.Stick

                let s', r' = step s a
                visits.Add((s, a))
                num_visits_pair[(s, a)] <- num_visits_pair.GetValueOrDefault((s, a), 0) + 1

                s <- s'
                r <- r + r'

            for i in 1 .. visits.Count - 1 do
                value_map[visits[i]] <- value_map.GetValueOrDefault(visits[i])
                                        + (1.0 / (double num_visits_pair[visits[i]]))
                                          * ((double r) - value_map.GetValueOrDefault(visits[i]))

        value_map

let td_training (q_optimal: Dictionary<Tuple<State, Action>, double>) =
    let errors = List<double>()

    for lambda in 0.0..0.1..1.0 do
        let value_map = Dictionary<Tuple<State, Action>, double>()
        let num_visits_pair = Dictionary<Tuple<State, Action>, int>()
        let num_visits = Dictionary<State, int>()
        let N0 = 100.0


        for iter in 1..5000 do
            let mutable s =
                { dealer_display = Random.Shared.Next(1, 11)
                  player_sum = Random.Shared.Next(1, 11) }

            let mutable r = 0.0

            let E_sa = Dictionary<Tuple<State, Action>, double>()

            while not (isTerminal s) do
                let epsilon = N0 / (N0 + double (num_visits.GetValueOrDefault(s, 0)))

                num_visits[s] <- num_visits.GetValueOrDefault(s, 0) + 1

                let a =
                    if Random.Shared.NextDouble() < epsilon then
                        enum<Action> (Random.Shared.Next(0, 2))
                    else if
                        value_map.GetValueOrDefault((s, Action.Hit), 0)
                        >= value_map.GetValueOrDefault((s, Action.Stick), 0)
                    then
                        Action.Hit
                    else
                        Action.Stick

                let s', r' = step s a
                num_visits_pair[(s, a)] <- num_visits_pair.GetValueOrDefault((s, a), 0) + 1

                let epsilon' = N0 / (N0 + double (num_visits.GetValueOrDefault(s', 0)))

                let a' =
                    if Random.Shared.NextDouble() < epsilon' then
                        enum<Action> (Random.Shared.Next(0, 2))
                    else if
                        value_map.GetValueOrDefault((s', Action.Hit), 0)
                        >= value_map.GetValueOrDefault((s', Action.Stick), 0)
                    then
                        Action.Hit
                    else
                        Action.Stick

                let error =
                    r' + value_map.GetValueOrDefault((s', a'), 0)
                    - value_map.GetValueOrDefault((s, a), 0)

                E_sa[(s, a)] <- E_sa.GetValueOrDefault((s, a), 0) + 1.0

                for pair in num_visits_pair.Keys do
                    value_map[pair] <- value_map.GetValueOrDefault(pair, 0)
                                       + (1.0 / (double (num_visits_pair.GetValueOrDefault(pair, 0))))
                                         * error
                                         * E_sa.GetValueOrDefault(pair, 0)

                    E_sa[pair] <- lambda * E_sa.GetValueOrDefault(pair, 0)


                s <- s'
                r <- r + r'

            if lambda = 0 || lambda = 1 then
                let mse =
                    seq { for qa in q_optimal.Keys -> (q_optimal[qa] - value_map.GetValueOrDefault(qa, 0)) ** 2 }
                    |> Seq.average

                ()
                printfn $"Lambda: %f{lambda} Iteration: {iter} Error: %f{mse}"

        let mse =
            seq { for qa in q_optimal.Keys -> (q_optimal[qa] - value_map.GetValueOrDefault(qa, 0)) ** 2 }
            |> Seq.average

        errors.Add(mse)

    for i in 0..10 do
        let mse = errors[i]
        printfn $"Lambda: %f{double i / 10.0} Error: %f{mse}"

let td_training_func (q_optimal: Dictionary<Tuple<State, Action>, double>) =
    let errors = List<double>()

    for lambda in 0.0..0.1..1.0 do

        let dealer_feature =
            [ fun (s: State) a -> s.dealer_display >= 1 && s.dealer_display <= 4
              fun (s: State) a -> s.dealer_display >= 4 && s.dealer_display <= 7
              fun (s: State) a -> s.dealer_display >= 7 && s.dealer_display <= 10 ]

        let player_feature =
            [ fun (s: State) a -> s.player_sum >= 1 && s.player_sum <= 6
              fun (s: State) a -> s.player_sum >= 4 && s.player_sum <= 9
              fun (s: State) a -> s.player_sum >= 7 && s.player_sum <= 12
              fun (s: State) a -> s.player_sum >= 10 && s.player_sum <= 15
              fun (s: State) a -> s.player_sum >= 13 && s.player_sum <= 18
              fun (s: State) a -> s.player_sum >= 16 && s.player_sum <= 21 ]

        let action_features =
            [ fun (S: State) (a: Action) -> a = Action.Hit
              fun (S: State) (a: Action) -> a = Action.Stick ]


        let features =
            seq {
                for i in dealer_feature do
                    for j in player_feature do
                        for k in action_features -> fun (s: State) (a: Action) -> i s a && j s a && k s a
            }
            |> Seq.map (fun f -> (f, 0.0))
            |> Seq.toArray

        let q_approx features (state: State) (action: Action) =
            seq { for select, value in features -> if select state action then value else 0.0 }
            |> Seq.sum


        let num_visits_pair = Dictionary<Tuple<State, Action>, int>()
        let num_visits = Dictionary<State, int>()
        let N0 = 100.0
        let epsilon = 0.05
        let alpha = 0.01


        for iter in 1..1000 do
            let mutable s =
                { dealer_display = Random.Shared.Next(1, 11)
                  player_sum = Random.Shared.Next(1, 11) }

            let mutable r = 0.0

            let E_sa = Dictionary<Tuple<State, Action>, double>()

            while not (isTerminal s) do

                num_visits[s] <- num_visits.GetValueOrDefault(s, 0) + 1

                let a =
                    if Random.Shared.NextDouble() < epsilon then
                        enum<Action> (Random.Shared.Next(0, 2))
                    else if q_approx features s Action.Hit >= q_approx features s Action.Stick then
                        Action.Hit
                    else
                        Action.Stick

                let s', r' = step s a
                num_visits_pair[(s, a)] <- num_visits_pair.GetValueOrDefault((s, a), 0) + 1

                let epsilon' = N0 / (N0 + double (num_visits.GetValueOrDefault(s', 0)))

                let a' =
                    if Random.Shared.NextDouble() < epsilon' then
                        enum<Action> (Random.Shared.Next(0, 2))
                    else if q_approx features s Action.Hit >= q_approx features s Action.Stick then
                        Action.Hit
                    else
                        Action.Stick

                let error = r' + (q_approx features s' a') - (q_approx features s a)

                E_sa[(s, a)] <- E_sa.GetValueOrDefault((s, a), 0) + 1.0

                for pair in num_visits_pair.Keys do
                    for i in 1 .. features.Length - 1 do
                        let selector, parameter = features[i]

                        if selector <|| pair then
                            let newParameter = parameter + alpha * error * E_sa.GetValueOrDefault(pair, 0)
                            features[i] <- (selector, newParameter)

                    E_sa[pair] <- lambda * E_sa.GetValueOrDefault(pair, 0)


                s <- s'
                r <- r + r'

            if lambda = 0 || lambda = 1 then
                let mse =
                    seq { for qa in q_optimal.Keys -> (q_optimal[qa] - (q_approx features <|| qa)) ** 2 }
                    |> Seq.average

                ()
                printfn $"Lambda: %.1f{lambda} Iteration: %4d{iter} Error: %f{mse}"

        let mse =
            seq { for qa in q_optimal.Keys -> (q_optimal[qa] - (q_approx features <|| qa)) ** 2 }
            |> Seq.average

        errors.Add(mse)

    for i in 0..10 do
        let mse = errors[i]
        printfn $"Lambda: %f{double i / 10.0} Error: %f{mse}"




type Mode =
    | Graph
    | TD
    | TD_func

let mode = Mode.TD_func

let map = monte_carlo_training.Value

if mode = Mode.TD then td_training map

if mode = Mode.TD_func then td_training_func map

if mode = Mode.Graph then
    let traces_x =
        [ for i in 1..10 ->
              Chart.Line3D(
                  [ for j in 1..20 ->
                        i,
                        j,
                        (map.GetValueOrDefault(({ dealer_display = i; player_sum = j }, Action.Hit), 0),
                         map.GetValueOrDefault(({ dealer_display = i; player_sum = j }, Action.Stick), 0))
                        ||> max ],
                  LineColor = Color.fromKeyword Blue
              ) ]

    let traces_y =
        [ for j in 1..20 ->
              Chart.Line3D(
                  [ for i in 1..10 ->
                        i,
                        j,
                        (map.GetValueOrDefault(({ dealer_display = i; player_sum = j }, Action.Hit), 0),
                         map.GetValueOrDefault(({ dealer_display = i; player_sum = j }, Action.Stick), 0))
                        ||> max ],
                  LineColor = Color.fromKeyword DarkBlue
              ) ]

    let traces = traces_x @ traces_y

    Chart.combine traces |> Chart.withSize (1500, 800) |> Chart.show
