using UnityEngine;

namespace Masu.UI
{
    public enum SortKind
    {
        Bubble = 0,
        Shaker = 1,
        Comb = 2,
        Gnome = 3,
        Selection = 4,
        Insertion = 5,
        Shell = 6,
        Quick = 7,
        Bucket = 8,
        Radix = 9,
        Heap = 10,
        Merge = 11,
    }

    public struct SortPanelState
    {
        public SortKind Kind;
        public int[] Values;
        public int CompareA;
        public int CompareB;
        public float SwapFlash;

        public int Phase;
        public int I;
        public int J;
        public int Gap;
        public int Lo;
        public int Hi;
        public int Pivot;
        public bool Forward;
        public int MinIdx;
        public int[] Aux;
        public int[] Count;
        public int[] BucketCursor;
        public int CollectB;
        public int HeapSize;
        public int MergeWidth;
        public int MergeLeft;
        public int MergeMid;
        public int MergeRight;
        public int MergeK;
        public int MergeI;
        public int MergeJ;
        public int[] QuickLo;
        public int[] QuickHi;
        public int QuickSp;
    }

    public static class SortPanelAlgorithms
    {
        static readonly SortKind[] PanelOrder =
        {
            SortKind.Bubble,
            SortKind.Shaker,
            SortKind.Comb,
            SortKind.Gnome,
            SortKind.Selection,
            SortKind.Insertion,
            SortKind.Shell,
            SortKind.Quick,
            SortKind.Bucket,
            SortKind.Radix,
            SortKind.Heap,
            SortKind.Merge,
        };

        public static SortKind KindForPanel(int panelIndex) =>
            PanelOrder[panelIndex % PanelOrder.Length];

        public static void Reset(ref SortPanelState s, int n, SortKind kind)
        {
            s.Kind = kind;
            s.Values = new int[n];
            for (int i = 0; i < n; i++) s.Values[i] = i + 1;
            Shuffle(s.Values);
            s.CompareA = -1;
            s.CompareB = -1;
            s.SwapFlash = 0f;
            s.Phase = 0;
            s.I = 0;
            s.J = 0;
            s.Gap = n;
            s.Lo = 0;
            s.Hi = n - 1;
            s.Forward = true;
            s.MinIdx = 0;
            s.Aux = null;
            s.Count = null;
            s.BucketCursor = null;
            s.CollectB = 0;
            s.HeapSize = n;
            s.MergeWidth = 1;
            s.MergeLeft = 0;
            s.MergeK = 0;

            switch (kind)
            {
                case SortKind.Comb:
                    s.Gap = n;
                    break;
                case SortKind.Shell:
                    s.Gap = ShellStartGap(n);
                    break;
                case SortKind.Quick:
                    s.QuickLo = new int[16];
                    s.QuickHi = new int[16];
                    s.QuickSp = 1;
                    s.QuickLo[0] = 0;
                    s.QuickHi[0] = n - 1;
                    s.Phase = 0;
                    break;
                case SortKind.Bucket:
                    s.Phase = 0;
                    s.I = 0;
                    s.Aux = new int[n];
                    break;
                case SortKind.Radix:
                    s.Phase = 0;
                    s.Gap = 1;
                    s.I = 0;
                    s.Count = new int[10];
                    s.Aux = new int[n];
                    break;
                case SortKind.Heap:
                    s.I = n / 2 - 1;
                    s.Phase = 0;
                    break;
                case SortKind.Merge:
                    s.MergeWidth = 1;
                    s.Phase = 0;
                    s.Aux = new int[n];
                    break;
                case SortKind.Insertion:
                    s.I = 1;
                    s.J = 1;
                    break;
                case SortKind.Shaker:
                    s.J = s.Lo;
                    break;
            }
        }

        public static void Step(ref SortPanelState s, float swapFlashDuration)
        {
            if (s.Values == null || s.Values.Length == 0) return;

            s.CompareA = -1;
            s.CompareB = -1;
            bool swapped = false;

            switch (s.Kind)
            {
                case SortKind.Bubble: swapped = StepBubble(ref s); break;
                case SortKind.Shaker: swapped = StepShaker(ref s); break;
                case SortKind.Comb: swapped = StepComb(ref s); break;
                case SortKind.Gnome: swapped = StepGnome(ref s); break;
                case SortKind.Selection: swapped = StepSelection(ref s); break;
                case SortKind.Insertion: swapped = StepInsertion(ref s); break;
                case SortKind.Shell: swapped = StepShell(ref s); break;
                case SortKind.Quick: swapped = StepQuick(ref s); break;
                case SortKind.Bucket: swapped = StepBucket(ref s); break;
                case SortKind.Radix: swapped = StepRadix(ref s); break;
                case SortKind.Heap: swapped = StepHeap(ref s); break;
                case SortKind.Merge: swapped = StepMerge(ref s); break;
            }

            if (swapped) s.SwapFlash = swapFlashDuration;
        }

        static void Shuffle(int[] arr)
        {
            for (int i = arr.Length - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (arr[i], arr[j]) = (arr[j], arr[i]);
            }
        }

        static void Swap(int[] a, int i, int j)
        {
            (a[i], a[j]) = (a[j], a[i]);
        }

        static int ShellStartGap(int n)
        {
            int h = 1;
            while (h < n / 3) h = h * 3 + 1;
            return h;
        }

        static bool StepBubble(ref SortPanelState s)
        {
            int n = s.Values.Length;
            int limit = n - 1 - s.I;
            if (limit <= 0)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.J >= limit)
            {
                s.I++;
                s.J = 0;
                if (s.I >= n - 1) Reset(ref s, n, s.Kind);
                return false;
            }

            int a = s.J;
            int b = s.J + 1;
            s.CompareA = a;
            s.CompareB = b;
            if (s.Values[a] > s.Values[b])
            {
                Swap(s.Values, a, b);
                s.J++;
                return true;
            }

            s.J++;
            return false;
        }

        static bool StepShaker(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.Lo >= s.Hi)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.Forward)
            {
                s.CompareA = s.J;
                s.CompareB = s.J + 1;
                if (s.Values[s.J] > s.Values[s.J + 1])
                {
                    Swap(s.Values, s.J, s.J + 1);
                    s.J++;
                    return true;
                }

                s.J++;
                if (s.J >= s.Hi)
                {
                    s.Hi--;
                    s.Forward = false;
                    s.J = s.Hi;
                }
            }
            else
            {
                s.CompareA = s.J - 1;
                s.CompareB = s.J;
                if (s.Values[s.J - 1] > s.Values[s.J])
                {
                    Swap(s.Values, s.J - 1, s.J);
                    s.J--;
                    return true;
                }

                s.J--;
                if (s.J <= s.Lo)
                {
                    s.Lo++;
                    s.Forward = true;
                    s.J = s.Lo;
                }
            }

            return false;
        }

        static bool StepComb(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.Gap < 1)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.J >= n - s.Gap)
            {
                s.Gap = Mathf.Max(1, (int)(s.Gap / 1.3f));
                s.J = 0;
                if (s.Gap == 1 && s.Phase == 1)
                {
                    Reset(ref s, n, s.Kind);
                    return false;
                }

                s.Phase = 0;
                return false;
            }

            int a = s.J;
            int b = s.J + s.Gap;
            s.CompareA = a;
            s.CompareB = b;
            if (s.Values[a] > s.Values[b])
            {
                Swap(s.Values, a, b);
                s.Phase = 1;
            }

            s.J++;
            return s.Phase == 1;
        }

        static bool StepGnome(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.J <= 0)
                s.J = 1;
            if (s.J >= n)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            s.CompareA = s.J - 1;
            s.CompareB = s.J;
            if (s.Values[s.J - 1] <= s.Values[s.J])
            {
                s.J++;
                return false;
            }

            Swap(s.Values, s.J - 1, s.J);
            s.J--;
            return true;
        }

        static bool StepSelection(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.I >= n - 1)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.Phase == 0)
            {
                s.MinIdx = s.I;
                s.J = s.I + 1;
                s.Phase = 1;
            }

            if (s.J < n)
            {
                s.CompareA = s.MinIdx;
                s.CompareB = s.J;
                if (s.Values[s.J] < s.Values[s.MinIdx])
                    s.MinIdx = s.J;
                s.J++;
                return false;
            }

            s.CompareA = s.I;
            s.CompareB = s.MinIdx;
            if (s.MinIdx != s.I)
                Swap(s.Values, s.I, s.MinIdx);
            s.I++;
            s.Phase = 0;
            return s.MinIdx != s.I;
        }

        static bool StepInsertion(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.I >= n)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.J <= 0)
            {
                s.I++;
                s.J = s.I;
                return false;
            }

            s.CompareA = s.J - 1;
            s.CompareB = s.J;
            if (s.Values[s.J - 1] <= s.Values[s.J])
            {
                s.I++;
                s.J = s.I;
                return false;
            }

            Swap(s.Values, s.J - 1, s.J);
            s.J--;
            return true;
        }

        static bool StepShell(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.Gap < 1)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.I >= n)
            {
                s.Gap /= 3;
                s.I = s.Gap;
                if (s.Gap < 1) Reset(ref s, n, s.Kind);
                return false;
            }

            int j = s.I;
            s.CompareA = j - s.Gap;
            s.CompareB = j;
            if (j >= s.Gap && s.Values[j - s.Gap] > s.Values[j])
            {
                Swap(s.Values, j - s.Gap, j);
                s.I++;
                return true;
            }

            s.I++;
            return false;
        }

        static bool StepQuick(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.QuickSp <= 0)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.Phase == 0)
            {
                s.QuickSp--;
                s.Lo = s.QuickLo[s.QuickSp];
                s.Hi = s.QuickHi[s.QuickSp];
                if (s.Lo >= s.Hi)
                    return false;
                s.Pivot = s.Hi;
                s.J = s.Lo;
                s.I = s.Lo - 1;
                s.Phase = 1;
                return false;
            }

            if (s.J < s.Hi)
            {
                s.CompareA = s.J;
                s.CompareB = s.Pivot;
                if (s.Values[s.J] < s.Values[s.Pivot])
                {
                    s.I++;
                    s.CompareA = s.I;
                    s.CompareB = s.J;
                    Swap(s.Values, s.I, s.J);
                    s.J++;
                    return true;
                }

                s.J++;
                return false;
            }

            int p = s.I + 1;
            s.CompareA = p;
            s.CompareB = s.Hi;
            Swap(s.Values, p, s.Hi);
            s.Phase = 0;
            if (s.Lo < p - 1 && s.QuickSp < s.QuickLo.Length)
            {
                s.QuickLo[s.QuickSp] = s.Lo;
                s.QuickHi[s.QuickSp] = p - 1;
                s.QuickSp++;
            }

            if (p + 1 < s.Hi && s.QuickSp < s.QuickLo.Length)
            {
                s.QuickLo[s.QuickSp] = p + 1;
                s.QuickHi[s.QuickSp] = s.Hi;
                s.QuickSp++;
            }

            return true;
        }

        static bool StepBucket(ref SortPanelState s)
        {
            int n = s.Values.Length;
            const int buckets = 4;

            if (s.Phase == 0)
            {
                if (s.I < n)
                {
                    s.CompareA = s.I;
                    s.CompareB = Mathf.Clamp(BucketIndex(s.Values[s.I], n, buckets), 0, n - 1);
                    s.I++;
                    return false;
                }

                int w = 0;
                for (int b = 0; b < buckets; b++)
                {
                    for (int i = 0; i < n; i++)
                    {
                        if (BucketIndex(s.Values[i], n, buckets) != b) continue;
                        s.Aux[w++] = s.Values[i];
                    }
                }

                for (int i = 0; i < n; i++)
                    s.Values[i] = s.Aux[i];
                Reset(ref s, n, s.Kind);
                return true;
            }

            return false;
        }

        static int BucketIndex(int v, int n, int buckets) =>
            Mathf.Clamp((v - 1) * buckets / n, 0, buckets - 1);

        static bool StepRadix(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.Gap > n)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.Phase == 0)
            {
                for (int i = 0; i < 10; i++) s.Count[i] = 0;
                for (int i = 0; i < n; i++)
                {
                    int d = (s.Values[i] / s.Gap) % 10;
                    s.Count[d]++;
                }

                int sum = 0;
                for (int d = 0; d < 10; d++)
                {
                    int c = s.Count[d];
                    s.Count[d] = sum;
                    sum += c;
                }

                s.I = 0;
                s.Phase = 1;
                return false;
            }

            if (s.I >= n)
            {
                for (int i = 0; i < n; i++)
                    s.Values[i] = s.Aux[i];
                s.Gap *= 10;
                s.Phase = 0;
                return false;
            }

            int v = s.Values[s.I];
            int digit = (v / s.Gap) % 10;
            s.CompareA = s.I;
            s.CompareB = digit;
            int pos = s.Count[digit]++;
            s.Aux[pos] = v;
            s.I++;
            return false;
        }

        static bool StepHeap(ref SortPanelState s)
        {
            int n = s.Values.Length;

            if (s.Phase == 0)
            {
                if (s.I < 0)
                {
                    s.Phase = 1;
                    s.HeapSize = n;
                    s.I = n - 1;
                    return false;
                }

                SiftDown(s.Values, n, s.I);
                s.CompareA = s.I;
                s.CompareB = Mathf.Min(s.I * 2 + 1, n - 1);
                s.I--;
                return false;
            }

            if (s.HeapSize <= 1)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            s.CompareA = 0;
            s.CompareB = s.HeapSize - 1;
            Swap(s.Values, 0, s.HeapSize - 1);
            s.HeapSize--;
            SiftDown(s.Values, s.HeapSize, 0);
            return true;
        }

        static void SiftDown(int[] a, int heapSize, int i)
        {
            while (true)
            {
                int l = i * 2 + 1;
                int r = l + 1;
                int largest = i;
                if (l < heapSize && a[l] > a[largest]) largest = l;
                if (r < heapSize && a[r] > a[largest]) largest = r;
                if (largest == i) break;
                Swap(a, i, largest);
                i = largest;
            }
        }

        static bool StepMerge(ref SortPanelState s)
        {
            int n = s.Values.Length;
            if (s.MergeWidth >= n)
            {
                Reset(ref s, n, s.Kind);
                return false;
            }

            if (s.Phase == 0)
            {
                s.MergeLeft = s.I;
                if (s.MergeLeft >= n)
                {
                    s.MergeWidth *= 2;
                    s.I = 0;
                    return false;
                }

                s.MergeMid = Mathf.Min(s.MergeLeft + s.MergeWidth, n);
                s.MergeRight = Mathf.Min(s.MergeLeft + s.MergeWidth * 2, n);
                s.MergeI = s.MergeLeft;
                s.MergeJ = s.MergeMid;
                s.MergeK = s.MergeLeft;
                s.Phase = 1;
                return false;
            }

            if (s.MergeK >= s.MergeRight)
            {
                for (int t = s.MergeLeft; t < s.MergeRight; t++)
                    s.Values[t] = s.Aux[t];
                s.I = s.MergeRight;
                s.Phase = 0;
                return false;
            }

            if (s.MergeI < s.MergeMid && (s.MergeJ >= s.MergeRight || s.Values[s.MergeI] <= s.Values[s.MergeJ]))
            {
                s.CompareA = s.MergeI;
                s.CompareB = s.MergeK;
                s.Aux[s.MergeK++] = s.Values[s.MergeI++];
            }
            else if (s.MergeJ < s.MergeRight)
            {
                s.CompareA = s.MergeJ;
                s.CompareB = s.MergeK;
                s.Aux[s.MergeK++] = s.Values[s.MergeJ++];
            }

            return false;
        }
    }
}
