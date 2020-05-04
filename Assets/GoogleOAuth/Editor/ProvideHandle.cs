using System;

namespace GoogleOAuth.Editor
{
    public  class ProvideHandle<T>
    {
        /// <summary>
        /// Completion event for the operation.
        /// This is called for both successful and failed cases.
        /// </summary>
        public event Action<ProvideHandle<T>> Completed;
        /// <summary>
        /// The result of the operation. 
        /// </summary>
        public T Result { get; private set; }
        /// <summary>
        /// The exception for a failed operation.
        /// </summary>
        public Exception OperationException { get; private set; }
        /// <summary>
        /// True if the operation is complete.
        /// </summary>
        public bool IsCompleted { get; private set; }
        /// <summary>
        /// True if the operation is failed.
        /// </summary>
        public bool IsFailed { get; private set; }
        
        internal void Success(T value)
        {
            Result = value;
            IsCompleted = true;
            Completed?.Invoke(this);
        }

        internal void Fail(Exception exception)
        {
            OperationException = exception;
            IsCompleted = true;
            IsFailed = true;
            Completed?.Invoke(null);
        }
    }

}