# RL with Reward Shaping in Unity 

This project implements a Unity-based grid navigation environment to evaluate how different reward shaping strategies affect reinforcement learning performance, focusing on convergence speed, stability, and policy efficiency.

## Environment

The agent operates in a expandable grid where it must:

- Collect a key (yellow cylinder)
- Reach the goal (green cube)

## Configuration:

- Start position, Key position and Goal position are editable in grid manager
- Obstacles vary by scenario
- Algorithm: Q-learning with ε-greedy policy

Four scenarios with different obstacle layouts are used, each trained for n number of episodes with a step limit of your choice.

## Project Structure

The project includes the following core components:

### Grid System
A fully managed grid environment with:
- Coordinate-based positioning
- Cell labeling and visualization
- Grid Manager for environment control
### Agent
An autonomous agent with:
- Visual representation in the scene
- Q-learning implementation
- Integration of reward functions (SR, DBR, DR)
### Utilities
Supporting tools including:
- Coordinate helper utilities
- Functions for state handling and environment interaction
## Reward Strategies
* Sparse Rewards (SR): Rewards only on key collection and goal completion, with a step penalty. Preserves optimality but slows learning.
* Distance-Based Rewards (DBR): Rewards based on Manhattan distance reduction. Improves guidance but may fail in blocked paths.
* Decaying Rewards (DR): Applies a decay factor λ / (λ + n) to reduce shaping over time, balancing guided exploration and autonomous learning.
## Hyperparameters
- Learning rate (α): 0.10–0.15
- Discount factor (γ): 0.97–0.99
- Epsilon (ε): 0.20–0.35
- Epsilon decay: 0.993–0.995
- Minimum epsilon: 0.01–0.05

##Anti-Stuck Mechanisms
To improve robustness in complex environments:

- Backtrack penalty
- Loop penalty
- Path-aware shaping with BFS validation

These mechanisms ensure the agent is not misled by invalid paths.

## Objective

To demonstrate that reinforcement learning performance depends not only on reward shaping, but on its design, temporal behavior, and consistency with the environment’s topology.
