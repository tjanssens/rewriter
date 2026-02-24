namespace TextRewriter.Core.Interfaces;

public interface IInputSimulator
{
    void SimulateCopy();
    void SimulateSelectAll();
    void SimulatePaste();
}
