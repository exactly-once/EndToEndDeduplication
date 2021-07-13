using System;

namespace ExactlyOnce.Core
{
    public class ProcessingResult<TResult>
    {
        readonly TResult value;
        readonly bool duplicate;

        public static ProcessingResult<TResult> Duplicate { get; } = new ProcessingResult<TResult>(default, true);

        public static ProcessingResult<TResult> Successful(TResult value)
        {
            return new ProcessingResult<TResult>(value, false);
        }

        public bool IsDuplicate => duplicate;

        public TResult Value
        {
            get
            {
                if (duplicate)
                {
                    throw new Exception("Duplicate result does not have a value.");
                }
                return value;
            }
        }

        ProcessingResult(TResult value, bool duplicate)
        {
            this.value = value;
            this.duplicate = duplicate;
        }
    }
}