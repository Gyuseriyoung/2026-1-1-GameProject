using System.Collections.Generic;
using System.Linq;

namespace RhythmSystem
{
    public class SimulatedMergeItem
    {
        public int type;
        public int index;
        public float simulationTime;
    }

    public class MergeSimulator
    {
        private readonly MergeObjectData mergeObjectData;

        public MergeSimulator(MergeObjectData data)
        {
            this.mergeObjectData = data;
        }

        public List<SimulatedMergeItem> RunSimulation(List<NoteData> notes)
        {
            List<SimulatedMergeItem> items = new List<SimulatedMergeItem>();

            foreach (var note in notes)
            {
                if (note.mergeType >= 0 && note.objectIndex >= 0)
                {
                    if (note.type == NoteType.Hold)
                    {
                        items.Add(new SimulatedMergeItem 
                        { 
                            type = note.mergeType, 
                            index = note.objectIndex, 
                            simulationTime = note.time 
                        });
                        items.Add(new SimulatedMergeItem 
                        { 
                            type = note.mergeType, 
                            index = note.objectIndex, 
                            simulationTime = note.time + note.length 
                        });
                    }
                    else
                    {
                        items.Add(new SimulatedMergeItem 
                        { 
                            type = note.mergeType, 
                            index = note.objectIndex, 
                            simulationTime = note.time 
                        });
                    }
                }
            }

            List<SimulatedMergeItem> sortedItems = items.OrderBy(i => i.simulationTime).ToList();
            List<SimulatedMergeItem> results = new List<SimulatedMergeItem>();

            foreach (var item in sortedItems)
            {
                results.Add(item);
                SimulateAutoMerge(results, item);
            }

            return results;
        }

        private void SimulateAutoMerge(List<SimulatedMergeItem> results, SimulatedMergeItem currentItem)
        {
            SimulatedMergeItem target = default;
            bool foundPair = false;

            foreach (var item in results)
            {
                if (item.type == currentItem.type && item.index == currentItem.index && !item.Equals(currentItem))
                {
                    target = item;
                    foundPair = true;
                    break;
                }
            }

            if (foundPair)
            {
                int nextIndex = currentItem.index + 1;
                int type = currentItem.type;

                if (type >= 0 && type < mergeObjectData.MergeData.Length)
                {
                    var dataList = mergeObjectData.MergeData[type].MergeDataList;
                    if (nextIndex >= dataList.Length)
                    {
                        return;
                    }
                }
                else
                {
                    return;
                }

                results.Remove(currentItem);
                results.Remove(target);

                SimulatedMergeItem nextItem = new SimulatedMergeItem 
                { 
                    type = type, 
                    index = nextIndex, 
                    simulationTime = currentItem.simulationTime 
                };
                results.Add(nextItem);

                SimulateAutoMerge(results, nextItem);
            }
        }
    }
}
