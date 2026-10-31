using System.Collections;
using LaundryLegends.Interaction;
using LaundryLegends.Laundry;
using UnityEngine;

namespace LaundryLegends.NPC
{
    /// <summary>Staff loop: drop-off -> washer -> dryer -> fold/wrap.</summary>
    public sealed class LaundryStaff : MonoBehaviour
    {
        private MachineLoadHook _washer;
        private MachineLoadHook _dryer;
        private FoldingStation _folding;
        private CarrySlot _dropOff;

        private IEnumerator Start()
        {
            _washer = FindByName("Machine_1");
            _dryer = FindByName("Machine_2");
            _folding = Object.FindFirstObjectByType<FoldingStation>();
            GameObject drop = GameObject.Find("DropOffSlot");
            _dropOff = drop != null ? drop.GetComponent<CarrySlot>() : null;

            while (true)
            {
                yield return new WaitUntil(() => _dropOff != null && !_dropOff.IsEmpty
                    && _washer != null && _washer.State == MachineState.Empty);

                LaundryItem item = _dropOff.Take();
                if (item == null || !_washer.LoadProcessedItem(item))
                {
                    yield return null;
                    continue;
                }

                _washer.StartCycle();
                yield return new WaitUntil(() => _washer.State == MachineState.Complete);
                item = _washer.TakeProcessedItem();
                if (item == null || _dryer == null || !_dryer.LoadProcessedItem(item))
                {
                    continue;
                }

                _dryer.StartCycle();
                yield return new WaitUntil(() => _dryer.State == MachineState.Complete);
                item = _dryer.TakeProcessedItem();
                if (item != null && _folding != null)
                {
                    _folding.WrapItem(item);
                }
            }
        }

        private static MachineLoadHook FindByName(string name)
        {
            GameObject target = GameObject.Find(name);
            return target != null ? target.GetComponent<MachineLoadHook>() : null;
        }
    }
}
