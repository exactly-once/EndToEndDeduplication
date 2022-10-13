using System;

namespace ExactlyOnce.Core
{
    public class ProcessingResult<TResult>
    {
        readonly TResult value;
        readonly bool duplicate;
        readonly bool failure;

        public static ProcessingResult<TResult> Duplicate { get; } = new ProcessingResult<TResult>(default, true, false);
        public static ProcessingResult<TResult> Failure { get; } = new ProcessingResult<TResult>(default, false, true);

        public static ProcessingResult<TResult> Successful(TResult value)
        {
            return new ProcessingResult<TResult>(value, false, false);
        }

        public bool IsDuplicate => duplicate;
        public bool IsFailure => failure;

        public TResult Value
        {
            get
            {
                if (duplicate || failure)
                {
                    throw new Exception("Duplicate result does not have a value.");
                }
                return value;
            }
        }

        ProcessingResult(TResult value, bool duplicate, bool failure)
        {
            this.value = value;
            this.duplicate = duplicate;
            this.failure = failure;
        }
    }
}