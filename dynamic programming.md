# Dynamic Programming

**Dynamic**: sequential or temporal component to the  problem 
**Programming**: optimising a “program”, i.e. a policy

## Planning in MDP

### Prediction
- **Input**: MDP and policy $\pi$
- **Output**: value function $v_\pi$

### Control
- **Input**: MDP
- **Output**: *optimal* value function $v_\pi$ and *optimal* policy $\pi$

## Method

### Iterative Policy Evaluation
- Prediction
- Evaluate the value function of states given the policy

## Principle of Optimality
A policy $\pi(a|s)$ achieves the optimal value from state s, $v_\pi(s) = v_∗(s)$ 
$$\iff$$
For any state $s^′$ reachable from $s$, $\pi$ achieves the optimal value from state $s^′$, $v_\pi(s^′) = v_∗(s^′)$

### Value Iteration
- Start from the final target and work backward

## Summary

| Problem    | Bellman Equation                                         | Algorithm                   |
| ---------- | -------------------------------------------------------- | --------------------------- |
| Prediction | Bellman Expectation Equation                             | Iterative Policy Evaluation |
| Control    | Bellman Expectation Equation + Greedy Policy Improvement | Policy Iteration            |
| Control    | Bellman Optimality Equation                              | Value Iteration             |
 

## Bellman Equation for MDP


$$
\begin{aligned}
    v(s)&=E[G_t|S_t=s] \\
        &=E[R_{t+1}+\gamma v(S_{t+1})]
\end{aligned}
$$

