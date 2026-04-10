public abstract interface Event
{
    private bool hasBegun = false;

    private bool finished = false;

    public bool IsFinished => finished;
    public bool HasBegun => hasBegun;

    public abstract void Begin();
    public abstract void FinishActions();

    public void Execute()
    {
        if (!hasBegun)
        {
            Begin();
            hasBegun = true;
        }
    }

    public void MarkFinished()
    {
        finished = true;
        FinishActions();
    }
}
