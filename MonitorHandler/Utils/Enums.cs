namespace MonitorHandler.Utils;

public enum TypeReceivedMessage
{
     Metric,
     AddedDockerImage,
     AddedDockerContainer,
     RemovedDockerImage,
     RemovedDockerContainer,
     UpdatedDockerContainer,
     Start,
     Result,
     Restarted
}

public enum TypeSentMessage
{
     StartContainer,
     StopContainer,
     RemoveContainer,
     RemoveImage,
     RunScript,
     RunCommand,
     Restart,
     Ok
}