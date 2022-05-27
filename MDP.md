## Return

The return $G_t$ is the total discounted reward from time-step $t$

$$G_t=R_{t+1}+\gamma R_{t+2}+...=\sum_{k=0}^\infty {\gamma^k R_{t+k+1}}$$

# Markov Decision Process

## Value Function

The state-value function $v_\pi(s)$ of an MDP is the expected return starting from state s, and then following policy $\pi$

$$v_\pi(s)=E[G_t|S=s]$$

## Action Value Function

The action-value function $q_\pi(s,a)$ of an MDP is the expected return starting from state s,taking action $a$, and then following policy $\pi$

$$q_\pi(s,a)=E[G_t|S=s, A=a]$$