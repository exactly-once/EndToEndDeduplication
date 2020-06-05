using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExactlyOnce.Entities.ClaimCheck.NServiceBus
{
    public class SideEffectsHandlerCollection
    {
        Dictionary<string, ISideEffectsHandler> handlers;

        public SideEffectsHandlerCollection(Dictionary<string, ISideEffectsHandler> handlers)
        {
            this.handlers = handlers;
        }

        public Task Prepare(IEnumerable<OutboxRecord> records)
        {
            return Handle(records, (h, set) => handlers[h].Prepare(set));
        }

        public Task Commit(IEnumerable<OutboxRecord> records)
        {
            return Handle(records, (h, set) => handlers[h].Commit(set));
        }

        static async Task Handle(IEnumerable<OutboxRecord> records, Func<string, List<OutboxRecord>, Task> callback)
        {
            var workingSet = new List<OutboxRecord>();
            string currentHandler = null;
            foreach (var record in records)
            {
                if (currentHandler == null)
                {
                    currentHandler = record.Type;
                    workingSet.Add(record);
                }
                else
                {
                    if (currentHandler == record.Type)
                    {
                        //Append next record to the working set
                        workingSet.Add(record);
                    }
                    else
                    {
                        //Push current working set
                        await callback(currentHandler, workingSet).ConfigureAwait(false);

                        //Start a new working set
                        workingSet.Clear();
                        currentHandler = record.Type;
                        workingSet.Add(record);
                    }
                }
            }
            //Push current working set
            if (currentHandler != null)
            {
                await callback(currentHandler, workingSet).ConfigureAwait(false);
            }
        }
    }
}