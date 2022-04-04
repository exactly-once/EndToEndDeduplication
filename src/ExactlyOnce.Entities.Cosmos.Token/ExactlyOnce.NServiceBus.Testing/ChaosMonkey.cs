namespace ExactlyOnce.NServiceBus.Testing
{
    using System;
    using System.Collections.Concurrent;
    using System.Linq;

    public class ChaosMonkey
    {
        readonly Func<string, int[], int[]> randomFailureGenerator;
        ConcurrentDictionary<string, ChaosTracker> trackers = new ConcurrentDictionary<string, ChaosTracker>();

        public ChaosMonkey(Func<string, int[], int[]> randomFailureGenerator)
        {
            this.randomFailureGenerator = randomFailureGenerator;
        }

        public void InvokeChaos<T>(string uniqueId, T value) where T : Enum
        {
            var tracker = trackers.GetOrAdd(uniqueId, _ =>
            {
                var allFailureModes = Enum.GetValues(typeof(T)).Cast<int>().ToArray();

                var selectedFailureModes = randomFailureGenerator(uniqueId, allFailureModes);
                var selectedFailureModeNames = selectedFailureModes.Select(x => Enum.GetName(typeof(T), x)).ToArray();
                var finalStep = allFailureModes.Last();

                return new ChaosTracker(selectedFailureModes, selectedFailureModeNames, finalStep);
            });
            var result = tracker.InvokeChaos((int)(object)value);
            if (result)
            {
                trackers.TryRemove(uniqueId, out _);
            }
        }
    }

    public class ChaosTracker
    {
        readonly int[] selectedFailureModes;
        readonly string[] selectedFailureModeNames;
        readonly int finalStep;
        int index;

        public ChaosTracker(int[] selectedFailureModes, string[] selectedFailureModeNames, int finalStep)
        {
            this.selectedFailureModes = selectedFailureModes;
            this.selectedFailureModeNames = selectedFailureModeNames;
            this.finalStep = finalStep;
        }

        public bool InvokeChaos(int step)
        {
            if (index < selectedFailureModes.Length && selectedFailureModes[index] == step)
            {
                index++;
                throw new SimulatedException("Failure during " + selectedFailureModeNames[index-1]);
            }

            if (step == finalStep)
            {
                return true;
            }

            return false;
        }
    }
}