public interface ITurnState
{
    void Enter(TurnManager manager);
    void Execute();
    void Exit();

}
