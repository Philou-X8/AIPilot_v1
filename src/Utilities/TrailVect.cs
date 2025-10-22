
using UnityGERunner;
using UnityGERunner.UnityApplication;

namespace AIPProvider.src.Utilities
{
    internal class TrailVect
    {
        private int maxSize;
        private List<Vector3> list;
        private int index; // index of last added element

        private int keyAdd;
        private int keyAvg;
        private Vector3 cashAvg;

        public TrailVect(int size)
        {
            maxSize = size;
            list = new List<Vector3>();
            index = -1;

            keyAdd = 0;
            keyAvg = 0;
            cashAvg = Vector3.zero;
        }

        public void Add(Vector3 v)
        {
            keyAdd++;
            index = (index + 1) % maxSize;

            if(list.Count < maxSize)
            {
                list.Add(v);
            }
            else
            {
                list[index] = v;
            }
        }

        public Vector3 Avg()
        {
            if (keyAvg == keyAdd) // check if the computed AVG is up to date;
            {
                return cashAvg;
            }

            if (list.Count < 1) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            foreach (Vector3 v in list)
            {
                sum += v;
            }

            cashAvg = sum * (1f / list.Count);
            keyAvg = keyAdd; // update key to avoid repeated computation
            return cashAvg;
        }

        public Vector3 AvgDelta()
        {
            if (list.Count < 2) return Vector3.zero;

            Vector3 sum = Vector3.zero;
            // march from oldest to right before the newest
            for(int i = index + 1; i < index + list.Count; i++)
            {
                //int left = i % list.Count;
                //int right = (i + 1) % list.Count;
                //Vector3 delta = list[right] - list[left];
                //sum += delta;
                sum += list[(i + 1) % list.Count] - list[i % list.Count];
            }
            return sum * (1f / (list.Count-1) );
        }

        public Vector3 Latest()
        {
            return list[index];
        }

    }
}
