namespace PetImages.Messaging.Worker
{
    public class WorkerResult
    {
        public WorkerResultCode ResultCode { get; set; }

        public string Message { get; set; }
    }

    public enum WorkerResultCode
    {
        Completed,
        Faulted,
        Enabled
    }
}
