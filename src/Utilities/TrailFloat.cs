using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AIPProvider.src.Utilities
{
    internal class TrailFloat
    {
        private int maxSize;
        private List<float> list;
        private int index; // index of last added element

        private int keyAdd;
        private int keyAvg;
        private float cashAvg;

        public TrailFloat(int size)
        {
            maxSize = size;
            list = new List<float>();
            index = -1;

            keyAdd = 0;
            keyAvg = 0;
            cashAvg = 0;
        }

        public void Add(float f)
        {
            keyAdd++;
            index = (index + 1) % maxSize;

            if (list.Count < maxSize)
            {
                list.Add(f);
            }
            else
            {
                list[index] = f;
            }
        }

        public float Avg()
        {
            if(keyAvg == keyAdd) // check if the computed AVG is up to date;
            {
                return cashAvg;
            }

            if(list.Count < 1) return 0;

            float sum = 0;
            foreach (float f in list)
            {
                sum += f;
            }
            cashAvg = sum * (1f / list.Count);
            keyAvg = keyAdd; // update key to avoid repeated computation
            return cashAvg;
        }

        public float AvgDelta()
        {
            if (list.Count < 2) return 0;

            float sum = 0;
            // march from oldest to right before the newest
            for (int i = index + 1; i < index + list.Count; i++)
            {
                //int left = i % list.Count;
                //int right = (i + 1) % list.Count;
                //Vector3 delta = list[right] - list[left];
                //sum += delta;
                sum += list[(i + 1) % list.Count] - list[i % list.Count];
            }
            return sum * (1f / (list.Count - 1));
        }

        public float Latest()
        {
            if (index >= 0 && index < list.Count)
            {

                return list[index];
            }
            else return 0;
        }
    }
}
