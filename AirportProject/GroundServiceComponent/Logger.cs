using System;
namespace GroundServiceComponent
{
    public interface ILogger
    {
        void Debug(string mes);
        void Info(string mes);
        void Error(string mes);
    }
    public class Logger : ILogger
    {
        public event Action<string> Write;
        public string className;

        public void Debug(string mes)
        {
            Write?.Invoke("Debug: " + mes);
        }
        public void Info(string mes)
        {
            Write?.Invoke("Info: " + mes);
        }
        public void Error(string mes)
        {
            Write?.Invoke("Error: " + mes);
            Info("Exit");
        }
    }
}
