using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEngine.EventSystems;
using System;
using System.Linq;

namespace UnityEngine.UI.Extension.Tools
{
    public class ItemsDescriptor
	{
		public float itemsConstantTransversalSize;
		public int itemsCount;
		public double cumulatedSizesOfAllItemsPlusSpacing;
		public int realIndexOfFirstItemInView;
		public int maxVisibleItemsSeenSinceLastScrollViewSizeChange = 0;
		public int destroyedItemsSinceLastScrollViewSizeChange = 0;

		public double CumulatedSizeOfAllItems { get { return itemsCount == 0 ? 0d : GetItemSizeCumulative(itemsCount - 1, false); } }

		public float this[int cellIndex]
		{
			get
            {
                float val;
                if (_Sizes.TryGetValue(cellIndex, out val))
                    return val;

                return _DefaultSize;
            }
			set
			{
				if (_ChangingItemsSizesInProgress)
				{
					if (cellIndex != _IndexInViewOfLastItemThatChangedSizeDuringSizesChange + 1)
						throw new UnityException("Sizes can only be changed for items one by one, one after another(e.g. 3,4,5,6,7..), starting with the one passed to BeginChangingItemsSizes(int)!");
					BinaryAddKeyToSortedListIfDoesntExist(cellIndex);
					_CumulatedSizesUntilNowDuringSizesChange += value;
					_Sizes[cellIndex] = value;
					_SizesCumulative[cellIndex] = _CumulatedSizesUntilNowDuringSizesChange;
					_IndexInViewOfLastItemThatChangedSizeDuringSizesChange = cellIndex;
				}
				else
					throw new UnityException("Call BeginChangingItemsSizes() before");
			}
		}

		List<int> _Keys = new List<int>();
		Dictionary<int, float> _Sizes = new Dictionary<int, float>(); // heights/widths
		Dictionary<int, double> _SizesCumulative = new Dictionary<int, double>(); // heights/widths
		float _DefaultSize;
		bool _ChangingItemsSizesInProgress;
		int _IndexInViewOfFirstItemThatChangesSizeDuringSizesChange;
		int _IndexInViewOfLastItemThatChangedSizeDuringSizesChange = -1;
		double _CumulatedSizesUntilNowDuringSizesChange;
		
		public ItemsDescriptor(float defaultSize)
		{
			ReinitializeSizes(ItemCountChangeMode.RESET, 0, -1, defaultSize);
		}


		public void ReinitializeSizes(ItemCountChangeMode changeMode, int count, int indexIfInsertingOrRemoving = -1, float? newDefaultSize = null)
		{
			if (newDefaultSize != null)
			{
				if (newDefaultSize != _DefaultSize)
				{
					if (changeMode != ItemCountChangeMode.RESET)
						throw new UnityException("Cannot preserve old sizes if the newDefaultSize is different!");

					_DefaultSize = newDefaultSize.Value;
				}
			}

			if (changeMode == ItemCountChangeMode.RESET)
			{
				_Sizes.Clear();
				_SizesCumulative.Clear();
				_Keys.Clear();
				itemsCount = count;

				return;
			}

			if (indexIfInsertingOrRemoving < 0 || indexIfInsertingOrRemoving > itemsCount)
				throw new ArgumentOutOfRangeException("indexIfInsertingOrRemoving", indexIfInsertingOrRemoving, "Should be positive and less than itemsCount=" + itemsCount);

			int newCount;
			if (changeMode == ItemCountChangeMode.INSERT)
			{
				ShiftSizesKeys(indexIfInsertingOrRemoving, count);

				newCount = itemsCount + count;
			}
			else
			{
				if (count < 0)
					throw new ArgumentOutOfRangeException("count", count, "Cannot be negative!");

				if (indexIfInsertingOrRemoving + count > itemsCount)
					throw new ArgumentOutOfRangeException("RemoveItems: index + count", indexIfInsertingOrRemoving + count, "Should be positive and less than- or or equal to itemsCount=" + itemsCount);

				count = -count;
				ShiftSizesKeys(indexIfInsertingOrRemoving, count);
				newCount = itemsCount + count;
			}
			itemsCount = newCount;
		}

		void BinaryAddKeyToSortedListIfDoesntExist(int key)
		{
			int indexOfKey = _Keys.BinarySearch(key);
			if (indexOfKey < 0) 
				_Keys.Insert(~indexOfKey, key);
		}

		void BinaryRemoveKeyFromSortedList(int key)
		{
			_Keys.RemoveAt(_Keys.BinarySearch(key));
		}

		void ShiftSizesKeys(int startingKey, int amount)
		{
			if (_Sizes.Count != _SizesCumulative.Count || _Sizes.Count != _Keys.Count)
				throw new InvalidOperationException("The sizes state was corrupted");
			
			var indexOfStartingKeyOrFirstKeyAfter = _Keys.BinarySearch(startingKey);
			if (indexOfStartingKeyOrFirstKeyAfter < 0) 
				indexOfStartingKeyOrFirstKeyAfter = ~indexOfStartingKeyOrFirstKeyAfter;

			int i = indexOfStartingKeyOrFirstKeyAfter;

			double contentSizeChange = 0d; // the shifting amount

			int key;
			float size;
			double sizeCumm;
			if (amount < 0)
			{
				int countBefore = _Keys.Count;
				int amountAbs = -amount;
				int lastItemIndexExclusive = startingKey + amountAbs;
				while (i < _Keys.Count && (key=_Keys[i]) < lastItemIndexExclusive)
				{
					contentSizeChange -= _Sizes[key];

					_Sizes.Remove(key);
					_SizesCumulative.Remove(key);
					_Keys.RemoveAt(i);
				}
				int itemsRemoved = countBefore - _Keys.Count;
				contentSizeChange -= (amountAbs - itemsRemoved) * _DefaultSize;

				for (; i < _Keys.Count; ++i)
				{
					key = _Keys[i];
					size = _Sizes[key];
					sizeCumm = _SizesCumulative[key];

					_Sizes.Remove(key);
					_SizesCumulative.Remove(key);

					var newKey = key + amount;
					if (newKey < 0) 
					{
						Debug.Log("here");
						_Keys.RemoveAt(i);
						continue;
					}
					_Keys[i] = newKey;
					_Sizes[newKey] = size;
					_SizesCumulative[newKey] = sizeCumm + contentSizeChange;
				}
			}
			else
			{
				contentSizeChange = amount * _DefaultSize; 

	
				int indexOfLeftMostKeyToBeShifted = i;
				for (i = _Keys.Count - 1; i >= indexOfLeftMostKeyToBeShifted; --i)
				{
					key = _Keys[i];
					size = _Sizes[key];
					sizeCumm = _SizesCumulative[key];

					_Sizes.Remove(key);
					_SizesCumulative.Remove(key);

					var newKey = key + amount;
					_Keys[i] = newKey;
					_Sizes[newKey] = size;
					_SizesCumulative[newKey] = sizeCumm + contentSizeChange;
				}
			}

		}

		public void BeginChangingItemsSizes(int indexInViewOfFirstItemThatWillChangeSize)
		{
			if (_ChangingItemsSizesInProgress)
				throw new UnityException("Call EndChangingItemsSizes() when done doing it");

			_ChangingItemsSizesInProgress = true;
            _IndexInViewOfFirstItemThatChangesSizeDuringSizesChange = indexInViewOfFirstItemThatWillChangeSize;
			_IndexInViewOfLastItemThatChangedSizeDuringSizesChange = _IndexInViewOfFirstItemThatChangesSizeDuringSizesChange - 1;
			_CumulatedSizesUntilNowDuringSizesChange = _IndexInViewOfFirstItemThatChangesSizeDuringSizesChange == 0 ? 0d : GetItemSizeCumulative(_IndexInViewOfFirstItemThatChangesSizeDuringSizesChange - 1);
		}

		public void EndChangingItemsSizes()
		{
			_ChangingItemsSizesInProgress = false;

			if (_IndexInViewOfLastItemThatChangedSizeDuringSizesChange == _IndexInViewOfFirstItemThatChangesSizeDuringSizesChange - 1)
				return; 

			var indexOfLastKeyThatChanged = _Keys.BinarySearch(_IndexInViewOfLastItemThatChangedSizeDuringSizesChange);
			if (indexOfLastKeyThatChanged < 0) // doesn't exist
				throw new InvalidOperationException("The sizes state was corrupted");

			double cumulatedSizesUntilNow = _CumulatedSizesUntilNowDuringSizesChange;
			int prevKey = _IndexInViewOfLastItemThatChangedSizeDuringSizesChange;
			int curKey;
			for (int i = indexOfLastKeyThatChanged + 1; i < _Keys.Count; ++i)
			{
				curKey = _Keys[i];
				cumulatedSizesUntilNow += (curKey - prevKey - 1) * _DefaultSize + _Sizes[curKey];
				_SizesCumulative[curKey] = cumulatedSizesUntilNow;
				prevKey = curKey;
			}
		}

		public int GetItemRealIndexFromViewIndex(int indexInView) { return (realIndexOfFirstItemInView + indexInView) % itemsCount; }
		public int GetItemViewIndexFromRealIndex(int realIndex) { return (realIndex - realIndexOfFirstItemInView + itemsCount) % itemsCount; }

		public double GetItemSizeCumulative(int cellIndex, bool allowInferringFromNeighborAfter = true)
		{
			
			if (_Keys.Count > 0)
			{
				double result;
				if (_SizesCumulative.TryGetValue(cellIndex, out result))
					return result;

				int indexOfNextKey = _Keys.BinarySearch(cellIndex);
				if (indexOfNextKey >= 0)
					throw new InvalidOperationException("The sizes state was corrupted. key not in _SizesCumulative, but present in _Keys");

				indexOfNextKey = ~indexOfNextKey;
				int indexOfPrevKey = indexOfNextKey - 1;
				if (indexOfNextKey < _Keys.Count && allowInferringFromNeighborAfter)
				{
					int indexInViewOfNextItemWithKnownSize = _Keys[indexOfNextKey];
					int itemsCountDeltaRight = indexInViewOfNextItemWithKnownSize - cellIndex;

					if ((indexOfPrevKey < 0 || itemsCountDeltaRight < (/*itemsCountDeltaLeft =*/ cellIndex - _Keys[indexOfPrevKey])))
						return _SizesCumulative[indexInViewOfNextItemWithKnownSize] - (this[indexInViewOfNextItemWithKnownSize] + (itemsCountDeltaRight - 1) * _DefaultSize);
				}

				if (indexOfPrevKey >= 0)
				{
					int indexInViewOfPrevItemWithKnownSize = _Keys[indexOfPrevKey];
					return _SizesCumulative[indexInViewOfPrevItemWithKnownSize] + (cellIndex - indexInViewOfPrevItemWithKnownSize) * _DefaultSize;
				}
			}


			return (cellIndex + 1) * _DefaultSize; // same as if there were no keys
		}

		public void RotateItemsSizesOnScrollViewLooped(int newValueOf_RealIndexOfFirstItemInView)
        {
            int oldValueOf_realIndexOfFirstItemInView = realIndexOfFirstItemInView;
            realIndexOfFirstItemInView = newValueOf_RealIndexOfFirstItemInView;

            int rotateAmount = oldValueOf_realIndexOfFirstItemInView - realIndexOfFirstItemInView;
			int keysCount = _Keys.Count;
			if (rotateAmount == 0 && keysCount == 0)
				return;
			if (rotateAmount < 0)
				rotateAmount += itemsCount;

			var keysOld = _Keys.ToArray();
			var sizesOld = _Sizes;
			_Keys.Clear();
			_Sizes = new Dictionary<int, float>(keysCount);
			_SizesCumulative.Clear();
			_SizesCumulative = new Dictionary<int, double>(keysCount);

			int oldKeyWithCurSize, newKeyWithCurSize;
			double cumulatedSizesOfAllItemsUntilNow = 0d;
			int prevKey = -1;
			int numGapsSinceLastKey;
			float size;
			for (int i = 0; i < keysCount; ++i)
			{
				oldKeyWithCurSize = keysOld[i];
				newKeyWithCurSize = (oldKeyWithCurSize + rotateAmount) % itemsCount;
				BinaryAddKeyToSortedListIfDoesntExist(newKeyWithCurSize);
				size = sizesOld[oldKeyWithCurSize];
				_Sizes[newKeyWithCurSize] = size;

				numGapsSinceLastKey = newKeyWithCurSize - prevKey - 1;
				cumulatedSizesOfAllItemsUntilNow += numGapsSinceLastKey * _DefaultSize;
				cumulatedSizesOfAllItemsUntilNow += size;
				_SizesCumulative[newKeyWithCurSize] = cumulatedSizesOfAllItemsUntilNow;

				prevKey = newKeyWithCurSize;
			}
        }
    }
}
