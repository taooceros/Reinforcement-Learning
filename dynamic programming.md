# Dynamic Programming

## Bellman Equation

$$
\begin{aligned}
    v(s)&=E[G_t|S_t=s]
        &=E[R_{t+1}+\gamma v(S_{t+1})]
\end{aligned}
$$