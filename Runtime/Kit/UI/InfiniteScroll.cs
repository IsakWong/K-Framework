// ----------------------------------------------------------------------------
// The MIT License
// InfiniteScroll https://github.com/mopsicus/infinite-scroll-unity
// Copyright (c) 2018-2021 Mopsicus <mail@mopsicus.ru>
// ----------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Mopsicus.InfiniteScroll
{
    /// <summary>
    /// Infinite scroller for long lists
    /// </summary>
    public class InfiniteScroll : MonoBehaviour, IDropHandler
    {
        /// <summary>
        /// Period for no-update list, if very fast add
        /// </summary>
        private const int UPDATE_TIME_DIFF = 500;

        /// <summary>
        /// Speed for scroll on move
        /// </summary>
        private const float SCROLL_SPEED = 50f;

        /// <summary>
        /// Duration for scroll move
        /// </summary>
        private const float SCROLL_DURATION = 0.25f;

        /// <summary>
        /// Load direction
        /// </summary>
        public enum Direction
        {
            Top = 0,
            Bottom = 1,
            Left = 2,
            Right = 3
        }

        /// <summary>
        /// Delegate for heights
        /// </summary>
        public delegate int HeightItem(int index);

        /// <summary>
        /// Event for get item height
        /// </summary>
        public event HeightItem OnHeight;

        /// <summary>
        /// Delegate for widths
        /// </summary>
        public delegate int WidthtItem(int index);

        /// <summary>
        /// Event for get item width
        /// </summary>
        public event HeightItem OnWidth;

        /// <summary>
        /// Callback on item fill
        /// </summary>
        public Action<int, GameObject> OnFill = delegate { };

        /// <summary>
        /// Callback on pull action
        /// </summary>
        public Action<Direction> OnPull = delegate { };

        [Header("Item settings")]
        /// <summary>
        /// Item list prefab
        /// </summary>
        public GameObject Prefab;

        [Header("Padding")]
        /// <summary>
        /// Top padding
        /// </summary>
        public int TopPadding = 10;

        /// <summary>
        /// Bottom padding
        /// </summary>
        public int BottomPadding = 10;

        [Header("Padding")]
        /// <summary>
        /// Left padding
        /// </summary>
        public int LeftPadding = 10;

        /// <summary>
        /// Right padding
        /// </summary>
        public int RightPadding = 10;

        /// <summary>
        /// Spacing between items
        /// </summary>
        public int ItemSpacing = 2;

        [Header("Labels")]
        /// <summary>
        /// Label font asset
        /// </summary>
        public TMP_FontAsset LabelsFont;

        /// <summary>
        /// Pull top text label
        /// </summary>
        public string TopPullLabel = "Pull to refresh";

        /// <summary>
        /// Release top text label
        /// </summary>
        public string TopReleaseLabel = "Release to load";

        /// <summary>
        /// Pull bottom text label
        /// </summary>
        public string BottomPullLabel = "Pull to refresh";

        /// <summary>
        /// Release bottom text label
        /// </summary>
        public string BottomReleaseLabel = "Release to load";

        /// <summary>
        /// Pull left text label
        /// </summary>
        public string LeftPullLabel = "Pull to refresh";

        /// <summary>
        /// Release left text label
        /// </summary>
        public string LeftReleaseLabel = "Release to load";

        /// <summary>
        /// Pull right text label
        /// </summary>
        public string RightPullLabel = "Pull to refresh";

        /// <summary>
        /// Release right text label
        /// </summary>
        public string RightReleaseLabel = "Release to load";

        [Header("Directions")]
        /// <summary>
        /// Can we pull from top
        /// </summary>
        public bool IsPullTop = true;

        /// <summary>
        /// Can we pull from bottom
        /// </summary>
        public bool IsPullBottom = true;

        [Header("Directions")]
        /// <summary>
        /// Can we pull from left
        /// </summary>
        public bool IsPullLeft = true;

        /// <summary>
        /// Can we pull from right
        /// </summary>
        public bool IsPullRight = true;

        [Header("Offsets")]
        /// <summary>
        /// Coefficient when labels should action
        /// </summary>
        public float PullValue = 1.5f;

        /// <summary>
        /// Label position offset
        /// </summary>
        public float LabelOffset = 85f;

        [HideInInspector]
        /// <summary>
        /// Top label
        /// </summary>
        public TextMeshProUGUI TopLabel;

        [HideInInspector]
        /// <summary>
        /// Bottom label
        /// </summary>
        public TextMeshProUGUI BottomLabel;

        [HideInInspector]
        /// <summary>
        /// Left label
        /// </summary>
        public TextMeshProUGUI LeftLabel;

        [HideInInspector]
        /// <summary>
        /// Right label
        /// </summary>
        public TextMeshProUGUI RightLabel;

        /// <summary>
        /// Type of scroller
        /// </summary>
        [HideInInspector] public int Type;

        /// <summary>
        /// Scrollrect cache
        /// </summary>
        private ScrollRect _scroll;

        /// <summary>
        /// Content rect cache
        /// </summary>
        private RectTransform _content;

        /// <summary>
        /// Container rect cache
        /// </summary>
        private Rect _container;

        /// <summary>
        /// All rects cache
        /// </summary>
        private RectTransform[] _rects;

        /// <summary>
        /// All objects cache
        /// </summary>
        private GameObject[] _views;

        /// <summary>
        /// State is can we pull from top
        /// </summary>
        private bool _isCanLoadUp;

        /// <summary>
        /// State is can we pull from bottom
        /// </summary>
        private bool _isCanLoadDown;

        /// <summary>
        /// State is can we pull from left
        /// </summary>
        private bool _isCanLoadLeft;

        /// <summary>
        /// State is can we pull from right
        /// </summary>
        private bool _isCanLoadRight;

        /// <summary>
        /// Previous position
        /// </summary>
        private int _previousPosition = -1;

        /// <summary>
        /// List items count
        /// </summary>
        private int _count;

        /// <summary>
        /// Items heights cache
        /// </summary>
        private Dictionary<int, int> _heights = new();

        /// <summary>
        /// Items widths cache
        /// </summary>
        private Dictionary<int, int> _widths = new();

        /// <summary>
        /// Items positions cache
        /// </summary>
        private Dictionary<int, float> _positions = new();

        /// <summary>
        /// Last manual move time to end
        /// </summary>
        private DateTime _lastMoveTime;

        /// <summary>
        /// Cache for scroll position
        /// </summary>
        private float _previousScrollPosition;

        /// <summary>
        /// Cache position for prevent sides effects
        /// </summary>
        private int _saveStepPosition = -1;

        /// <summary>
        /// Constructor
        /// </summary>
        private void Awake()
        {
            _container = GetComponent<RectTransform>().rect;
            _scroll = GetComponent<ScrollRect>();
            _scroll.onValueChanged.AddListener(OnScrollChange);
            _content = _scroll.viewport.transform.GetChild(0).GetComponent<RectTransform>();
            CreateLabels();
        }

        /// <summary>
        /// Main loop to check items positions and heights
        /// </summary>
        private void Update()
        {
            if (Type == 0)
            {
                UpdateVertical();
            }
            else
            {
                UpdateHorizontal();
            }
        }

        /// <summary>
        /// Main loop for vertical
        /// </summary>
        private void UpdateVertical()
        {
            if (_count == 0)
            {
                return;
            }

            var _topPosition = _content.anchoredPosition.y - ItemSpacing;
            if (_topPosition <= 0f && _rects[0].anchoredPosition.y < -TopPadding - 10f)
            {
                InitData(_count);
                return;
            }

            if (_topPosition < 0f)
            {
                return;
            }

            if (!_positions.ContainsKey(_previousPosition) || !_heights.ContainsKey(_previousPosition))
            {
                return;
            }

            var itemPosition = Mathf.Abs(_positions[_previousPosition]) + _heights[_previousPosition];
            var position = _topPosition > itemPosition ? _previousPosition + 1 : _previousPosition - 1;
            var border = (int)(_positions[0] + _heights[0]);
            var step = (int)((_topPosition + _topPosition / 1.25f) / border);
            if (step != _saveStepPosition)
            {
                _saveStepPosition = step;
            }
            else
            {
                return;
            }

            if (position < 0 || _previousPosition == position || _scroll.velocity.y == 0f)
            {
                return;
            }

            if (position > _previousPosition)
            {
                if (position - _previousPosition > 1)
                {
                    position = _previousPosition + 1;
                }

                var newPosition = position % _views.Length;
                newPosition--;
                if (newPosition < 0)
                {
                    newPosition = _views.Length - 1;
                }

                var index = position + _views.Length - 1;
                if (index < _count)
                {
                    var pos = _rects[newPosition].anchoredPosition;
                    pos.y = _positions[index];
                    _rects[newPosition].anchoredPosition = pos;
                    var size = _rects[newPosition].sizeDelta;
                    size.y = _heights[index];
                    _rects[newPosition].sizeDelta = size;
                    _views[newPosition].name = index.ToString();
                    OnFill(index, _views[newPosition]);
                }
            }
            else
            {
                if (_previousPosition - position > 1)
                {
                    position = _previousPosition - 1;
                }

                var newIndex = position % _views.Length;
                var pos = _rects[newIndex].anchoredPosition;
                pos.y = _positions[position];
                _rects[newIndex].anchoredPosition = pos;
                var size = _rects[newIndex].sizeDelta;
                size.y = _heights[position];
                _rects[newIndex].sizeDelta = size;
                _views[newIndex].name = position.ToString();
                OnFill(position, _views[newIndex]);
            }

            _previousPosition = position;
        }

        /// <summary>
        /// Main loop for horizontal
        /// </summary>
        private void UpdateHorizontal()
        {
            if (_count == 0)
            {
                return;
            }

            var _leftPosition = _content.anchoredPosition.x * -1f - ItemSpacing;
            if (_leftPosition <= 0f && _rects[0].anchoredPosition.x < -LeftPadding - 10f)
            {
                InitData(_count);
                return;
            }

            if (_leftPosition < 0f)
            {
                return;
            }

            if (!_positions.ContainsKey(_previousPosition) || !_widths.ContainsKey(_previousPosition))
            {
                return;
            }

            var itemPosition = Mathf.Abs(_positions[_previousPosition]) + _widths[_previousPosition];
            var position = _leftPosition > itemPosition ? _previousPosition + 1 : _previousPosition - 1;
            var border = (int)(_positions[0] + _widths[0]);
            var step = (int)((_leftPosition + _leftPosition / 1.25f) / border);
            if (step != _saveStepPosition)
            {
                _saveStepPosition = step;
            }
            else
            {
                return;
            }

            if (position < 0 || _previousPosition == position || _scroll.velocity.x == 0f)
            {
                return;
            }

            if (position > _previousPosition)
            {
                if (position - _previousPosition > 1)
                {
                    position = _previousPosition + 1;
                }

                var newPosition = position % _views.Length;
                newPosition--;
                if (newPosition < 0)
                {
                    newPosition = _views.Length - 1;
                }

                var index = position + _views.Length - 1;
                if (index < _count)
                {
                    var pos = _rects[newPosition].anchoredPosition;
                    pos.x = _positions[index];
                    _rects[newPosition].anchoredPosition = pos;
                    var size = _rects[newPosition].sizeDelta;
                    size.x = _widths[index];
                    _rects[newPosition].sizeDelta = size;
                    _views[newPosition].name = index.ToString();
                    OnFill(index, _views[newPosition]);
                }
            }
            else
            {
                if (_previousPosition - position > 1)
                {
                    position = _previousPosition - 1;
                }

                var newIndex = position % _views.Length;
                var pos = _rects[newIndex].anchoredPosition;
                pos.x = _positions[position];
                _rects[newIndex].anchoredPosition = pos;
                var size = _rects[newIndex].sizeDelta;
                size.x = _widths[position];
                _rects[newIndex].sizeDelta = size;
                _views[newIndex].name = position.ToString();
                OnFill(position, _views[newIndex]);
            }

            _previousPosition = position;
        }

        /// <summary>
        /// Handler on scroller
        /// </summary>
        private void OnScrollChange(Vector2 vector)
        {
            if (Type == 0)
            {
                ScrollChangeVertical(vector);
            }
            else
            {
                ScrollChangeHorizontal(vector);
            }
        }

        /// <summary>
        /// Handler on vertical scroll change
        /// </summary>
        private void ScrollChangeVertical(Vector2 vector)
        {
            _isCanLoadUp = false;
            _isCanLoadDown = false;
            if (_views == null)
            {
                return;
            }

            var y = 0f;
            var z = 0f;
            var isScrollable = _scroll.verticalNormalizedPosition != 1f && _scroll.verticalNormalizedPosition != 0f;
            y = _content.anchoredPosition.y;
            if (isScrollable)
            {
                if (_scroll.verticalNormalizedPosition < 0f)
                {
                    z = y - _previousScrollPosition;
                }
                else
                {
                    _previousScrollPosition = y;
                }
            }
            else
            {
                z = y;
            }

            if (y < -LabelOffset && IsPullTop)
            {
                TopLabel.gameObject.SetActive(true);
                TopLabel.text = TopPullLabel;
                if (y < -LabelOffset * PullValue)
                {
                    TopLabel.text = TopReleaseLabel;
                    _isCanLoadUp = true;
                }
            }
            else
            {
                TopLabel.gameObject.SetActive(false);
            }

            if (z > LabelOffset && IsPullBottom)
            {
                BottomLabel.gameObject.SetActive(true);
                BottomLabel.text = BottomPullLabel;
                if (z > LabelOffset * PullValue)
                {
                    BottomLabel.text = BottomReleaseLabel;
                    _isCanLoadDown = true;
                }
            }
            else
            {
                BottomLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Handler on horizontal scroll change
        /// </summary>
        private void ScrollChangeHorizontal(Vector2 vector)
        {
            _isCanLoadLeft = false;
            _isCanLoadRight = false;
            if (_views == null)
            {
                return;
            }

            var x = 0f;
            var z = 0f;
            var isScrollable =
                _scroll.horizontalNormalizedPosition != 1f && _scroll.horizontalNormalizedPosition != 0f;
            x = _content.anchoredPosition.x;
            if (isScrollable)
            {
                if (_scroll.horizontalNormalizedPosition > 1f)
                {
                    z = x - _previousScrollPosition;
                }
                else
                {
                    _previousScrollPosition = x;
                }
            }
            else
            {
                z = x;
            }

            if (x > LabelOffset && IsPullLeft)
            {
                LeftLabel.gameObject.SetActive(true);
                LeftLabel.text = LeftPullLabel;
                if (x > LabelOffset * PullValue)
                {
                    LeftLabel.text = LeftReleaseLabel;
                    _isCanLoadLeft = true;
                }
            }
            else
            {
                LeftLabel.gameObject.SetActive(false);
            }

            if (z < -LabelOffset && IsPullRight)
            {
                RightLabel.gameObject.SetActive(true);
                RightLabel.text = RightPullLabel;
                if (z < -LabelOffset * PullValue)
                {
                    RightLabel.text = RightReleaseLabel;
                    _isCanLoadRight = true;
                }
            }
            else
            {
                RightLabel.gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Hander on scroller drop pull
        /// </summary>
        public void OnDrop(PointerEventData eventData)
        {
            if (Type == 0)
            {
                DropVertical();
            }
            else
            {
                DropHorizontal();
            }
        }

        /// <summary>
        /// Handler on scroller vertical drop
        /// </summary>
        private void DropVertical()
        {
            if (_isCanLoadUp)
            {
                OnPull(Direction.Top);
            }
            else if (_isCanLoadDown)
            {
                OnPull(Direction.Bottom);
            }

            _isCanLoadUp = false;
            _isCanLoadDown = false;
        }

        /// <summary>
        /// Handler on scroller horizontal drop
        /// </summary>
        private void DropHorizontal()
        {
            if (_isCanLoadLeft)
            {
                OnPull(Direction.Left);
            }
            else if (_isCanLoadRight)
            {
                OnPull(Direction.Right);
            }

            _isCanLoadLeft = false;
            _isCanLoadRight = false;
        }

        /// <summary>
        /// Init list
        /// </summary>
        /// <param name="count">Items count</param>
        public void InitData(int count)
        {
            if (Type == 0)
            {
                InitVertical(count);
            }
            else
            {
                InitHorizontal(count);
            }
        }

        /// <summary>
        /// Init vertical list
        /// </summary>
        /// <param name="count">Item count</param>
        private void InitVertical(int count)
        {
            var height = CalcSizesPositions(count);
            CreateViews();
            _previousPosition = 0;
            _count = count;
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, height);
            var pos = _content.anchoredPosition;
            var size = Vector2.zero;
            pos.y = 0f;
            _content.anchoredPosition = pos;
            var y = TopPadding;
            var showed = false;
            for (var i = 0; i < _views.Length; i++)
            {
                showed = i < count;
                _views[i].gameObject.SetActive(showed);
                if (i + 1 > _count)
                {
                    continue;
                }

                pos = _rects[i].anchoredPosition;
                pos.y = _positions[i];
                pos.x = 0f;
                _rects[i].anchoredPosition = pos;
                size = _rects[i].sizeDelta;
                size.y = _heights[i];
                _rects[i].sizeDelta = size;
                y += ItemSpacing + _heights[i];
                _views[i].name = i.ToString();
                OnFill(i, _views[i]);
            }
        }

        /// <summary>
        /// Init horizontal list
        /// </summary>
        /// <param name="count">Item count</param>
        private void InitHorizontal(int count)
        {
            var width = CalcSizesPositions(count);
            CreateViews();
            _previousPosition = 0;
            _count = count;
            _content.sizeDelta = new Vector2(width, _content.sizeDelta.y);
            var pos = _content.anchoredPosition;
            var size = Vector2.zero;
            pos.x = 0f;
            _content.anchoredPosition = pos;
            var x = LeftPadding;
            var showed = false;
            for (var i = 0; i < _views.Length; i++)
            {
                showed = i < count;
                _views[i].gameObject.SetActive(showed);
                if (i + 1 > _count)
                {
                    continue;
                }

                pos = _rects[i].anchoredPosition;
                pos.x = _positions[i];
                pos.y = 0f;
                _rects[i].anchoredPosition = pos;
                size = _rects[i].sizeDelta;
                size.x = _widths[i];
                _rects[i].sizeDelta = size;
                x += ItemSpacing + _widths[i];
                _views[i].name = i.ToString();
                OnFill(i, _views[i]);
            }
        }

        /// <summary>
        /// Calc all items height and positions
        /// </summary>
        /// <returns>Common content height</returns>
        private float CalcSizesPositions(int count)
        {
            return Type == 0 ? CalcSizesPositionsVertical(count) : CalcSizesPositionsHorizontal(count);
        }

        /// <summary>
        /// Calc all items height and positions
        /// </summary>
        /// <returns>Common content height</returns>
        private float CalcSizesPositionsVertical(int count)
        {
            _heights.Clear();
            _positions.Clear();
            var result = 0f;
            for (var i = 0; i < count; i++)
            {
                _heights[i] = OnHeight(i);
                _positions[i] = -(TopPadding + i * ItemSpacing + result);
                result += _heights[i];
            }

            result += TopPadding + BottomPadding + (count == 0 ? 0 : (count - 1) * ItemSpacing);
            return result;
        }

        /// <summary>
        /// Calc all items width and positions
        /// </summary>
        /// <returns>Common content width</returns>
        private float CalcSizesPositionsHorizontal(int count)
        {
            _widths.Clear();
            _positions.Clear();
            var result = 0f;
            for (var i = 0; i < count; i++)
            {
                _widths[i] = OnWidth(i);
                _positions[i] = LeftPadding + i * ItemSpacing + result;
                result += _widths[i];
            }

            result += LeftPadding + RightPadding + (count == 0 ? 0 : (count - 1) * ItemSpacing);
            return result;
        }

        /// <summary>
        /// Update list after load new items
        /// </summary>
        /// <param name="count">Total items count</param>
        /// <param name="newCount">Added items count</param>
        /// <param name="direction">Direction to add</param>
        public void ApplyDataTo(int count, int newCount, Direction direction)
        {
            if (Type == 0)
            {
                ApplyDataToVertical(count, newCount, direction);
            }
            else
            {
                ApplyDataToHorizontal(count, newCount, direction);
            }
        }

        /// <summary>
        /// Update list after load new items for vertical scroller
        /// </summary>
        /// <param name="count">Total items count</param>
        /// <param name="newCount">Added items count</param>
        /// <param name="direction">Direction to add</param>
        private void ApplyDataToVertical(int count, int newCount, Direction direction)
        {
            _count = count;
            if (_count <= _views.Length)
            {
                InitData(count);
                return;
            }

            var height = CalcSizesPositions(count);
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, height);
            var pos = _content.anchoredPosition;
            if (direction == Direction.Top)
            {
                var y = 0f;
                for (var i = 0; i < newCount; i++)
                {
                    y += _heights[i] + ItemSpacing;
                }

                pos.y = y;
                _previousPosition = newCount;
            }
            else
            {
                var h = 0f;
                for (var i = _heights.Count - 1; i >= _heights.Count - newCount; i--)
                {
                    h += _heights[i] + ItemSpacing;
                }

                pos.y = height - h - _container.height;
            }

            _content.anchoredPosition = pos;
            var _topPosition = _content.anchoredPosition.y - ItemSpacing;
            var itemPosition = Mathf.Abs(_positions[_previousPosition]) + _heights[_previousPosition];
            var position = _topPosition > itemPosition ? _previousPosition + 1 : _previousPosition - 1;
            if (position < 0)
            {
                _previousPosition = 0;
                position = 1;
            }

            for (var i = 0; i < _views.Length; i++)
            {
                var newIndex = position % _views.Length;
                if (newIndex < 0)
                {
                    continue;
                }

                _views[newIndex].gameObject.SetActive(true);
                _views[newIndex].name = position.ToString();
                OnFill(position, _views[newIndex]);
                pos = _rects[newIndex].anchoredPosition;
                pos.y = _positions[position];
                _rects[newIndex].anchoredPosition = pos;
                var size = _rects[newIndex].sizeDelta;
                size.y = _heights[position];
                _rects[newIndex].sizeDelta = size;
                position++;
                if (position == _count)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Update list after load new items for horizontal scroller
        /// </summary>
        /// <param name="count">Total items count</param>
        /// <param name="newCount">Added items count</param>
        /// <param name="direction">Direction to add</param>
        private void ApplyDataToHorizontal(int count, int newCount, Direction direction)
        {
            _count = count;
            if (_count <= _views.Length)
            {
                InitData(count);
                return;
            }

            var width = CalcSizesPositions(count);
            _content.sizeDelta = new Vector2(width, _content.sizeDelta.y);
            var pos = _content.anchoredPosition;
            if (direction == Direction.Left)
            {
                var x = 0f;
                for (var i = 0; i < newCount; i++)
                {
                    x -= _widths[i] + ItemSpacing;
                }

                pos.x = x;
                _previousPosition = newCount;
            }
            else
            {
                var w = 0f;
                for (var i = _widths.Count - 1; i >= _widths.Count - newCount; i--)
                {
                    w += _widths[i] + ItemSpacing;
                }

                pos.x = -width + w + _container.width;
            }

            _content.anchoredPosition = pos;
            var _leftPosition = _content.anchoredPosition.x - ItemSpacing;
            var itemPosition = Mathf.Abs(_positions[_previousPosition]) + _widths[_previousPosition];
            var position = _leftPosition > itemPosition ? _previousPosition + 1 : _previousPosition - 1;
            if (position < 0)
            {
                _previousPosition = 0;
                position = 1;
            }

            for (var i = 0; i < _views.Length; i++)
            {
                var newIndex = position % _views.Length;
                if (newIndex < 0)
                {
                    continue;
                }

                _views[newIndex].gameObject.SetActive(true);
                _views[newIndex].name = position.ToString();
                OnFill(position, _views[newIndex]);
                pos = _rects[newIndex].anchoredPosition;
                pos.x = _positions[position];
                _rects[newIndex].anchoredPosition = pos;
                var size = _rects[newIndex].sizeDelta;
                size.x = _widths[position];
                _rects[newIndex].sizeDelta = size;
                position++;
                if (position == _count)
                {
                    break;
                }
            }
        }

        /// <summary>
        /// Update list after items delete
        /// </summary>
        /// <param name="index">Index to move from</param>
        /// <param name="height">New height</param>
        private void MoveDataTo(int index, float height)
        {
            if (Type == 0)
            {
                MoveDataToVertical(index, height);
            }
            else
            {
                MoveDataToHorizontal(index, height);
            }
        }

        /// <summary>
        /// Update list after items delete for vertical scroller
        /// </summary>
        /// <param name="index">Index to move from</param>
        /// <param name="height">New height</param>
        private void MoveDataToVertical(int index, float height)
        {
            _content.sizeDelta = new Vector2(_content.sizeDelta.x, height);
            var pos = _content.anchoredPosition;
            for (var i = 0; i < _views.Length; i++)
            {
                var newIndex = index % _views.Length;
                _views[newIndex].name = index.ToString();
                if (index >= _count)
                {
                    _views[newIndex].gameObject.SetActive(false);
                    continue;
                }
                else
                {
                    _views[newIndex].gameObject.SetActive(true);
                    OnFill(index, _views[newIndex]);
                }

                pos = _rects[newIndex].anchoredPosition;
                pos.y = _positions[index];
                _rects[newIndex].anchoredPosition = pos;
                var size = _rects[newIndex].sizeDelta;
                size.y = _heights[index];
                _rects[newIndex].sizeDelta = size;
                index++;
            }
        }

        /// <summary>
        /// Update list after items delete for horizontal scroller
        /// </summary>
        /// <param name="index">Index to move from</param>
        /// <param name="width">New width</param>
        private void MoveDataToHorizontal(int index, float width)
        {
            _content.sizeDelta = new Vector2(width, _content.sizeDelta.y);
            var pos = _content.anchoredPosition;
            for (var i = 0; i < _views.Length; i++)
            {
                var newIndex = index % _views.Length;
                _views[newIndex].name = index.ToString();
                if (index >= _count)
                {
                    _views[newIndex].gameObject.SetActive(false);
                    continue;
                }
                else
                {
                    _views[newIndex].gameObject.SetActive(true);
                    OnFill(index, _views[newIndex]);
                }

                pos = _rects[newIndex].anchoredPosition;
                pos.x = _positions[index];
                _rects[newIndex].anchoredPosition = pos;
                var size = _rects[newIndex].sizeDelta;
                size.x = _widths[index];
                _rects[newIndex].sizeDelta = size;
                index++;
            }
        }

        /// <summary>
        /// Move scroll to side
        /// </summary>
        /// <param name="direction">Direction to move</param>
        public void MoveToSide(Direction direction)
        {
            var now = DateTime.Now;
            if ((now - _lastMoveTime).TotalMilliseconds < UPDATE_TIME_DIFF)
            {
                return;
            }

            _lastMoveTime = now;
            StartCoroutine(MoveTo(direction));
        }

        /// <summary>
        /// Move coroutine
        /// </summary>
        /// <param name="direction">Direction to move</param>
        private IEnumerator MoveTo(Direction direction)
        {
            var speed = SCROLL_SPEED;
            var start = 0f;
            var end = 0f;
            var timer = 0f;
            if (Type == 0)
            {
                start = _scroll.verticalNormalizedPosition;
                end = direction == Direction.Bottom ? 0f : 1f;
            }
            else
            {
                start = _scroll.horizontalNormalizedPosition;
                end = direction == Direction.Left ? 0f : 1f;
            }

            while (timer <= 1f)
            {
                speed = Mathf.Lerp(speed, 0f, timer);
                if (Type == 0)
                {
                    _scroll.verticalNormalizedPosition = Mathf.Lerp(start, end, timer);
                    _scroll.velocity = new Vector2(0f, direction == Direction.Top ? -speed : speed);
                }
                else
                {
                    _scroll.horizontalNormalizedPosition = Mathf.Lerp(start, end, timer);
                    _scroll.velocity = new Vector2(direction == Direction.Left ? speed : -speed, 0f);
                }

                timer += Time.deltaTime / SCROLL_DURATION;
                yield return null;
            }

            if (Type == 0)
            {
                _scroll.velocity = new Vector2(0f, direction == Direction.Top ? -SCROLL_SPEED : SCROLL_SPEED);
            }
            else
            {
                _scroll.velocity = new Vector2(direction == Direction.Left ? SCROLL_SPEED : -SCROLL_SPEED, 0f);
            }
        }

        /// <summary>
        /// Disable all items in list
        /// </summary>
        public void RecycleAll()
        {
            _count = 0;
            if (_views == null)
            {
                return;
            }

            for (var i = 0; i < _views.Length; i++)
            {
                _views[i].gameObject.SetActive(false);
            }
        }

        /// <summary>
        /// Disable item
        /// </summary>
        /// <param name="index">Index in list data</param>
        public void Recycle(int index)
        {
            _count--;
            var name = index.ToString();
            var height = CalcSizesPositions(_count);
            for (var i = 0; i < _views.Length; i++)
            {
                if (string.CompareOrdinal(_views[i].name, name) == 0)
                {
                    _views[i].gameObject.SetActive(false);
                    MoveDataTo(i, height);
                    break;
                }
            }
        }

        /// <summary>
        /// Update visible items with new data
        /// </summary>
        public void UpdateVisible()
        {
            var showed = false;
            for (var i = 0; i < _views.Length; i++)
            {
                showed = i < _count;
                _views[i].gameObject.SetActive(showed);
                if (i + 1 > _count)
                {
                    continue;
                }

                var index = int.Parse(_views[i].name);
                OnFill(index, _views[i]);
            }
        }

        /// <summary>
        /// Clear views cache
        /// Needed to recreate views after Prefab change
        /// </summary>
        public void RefreshViews()
        {
            if (_views == null)
            {
                return;
            }

            for (var i = 0; i < _views.Length; i++)
            {
                Destroy(_views[i].gameObject);
            }

            _rects = null;
            _views = null;
            CreateViews();
        }

        /// <summary>
        /// Create views
        /// </summary>
        private void CreateViews()
        {
            if (Type == 0)
            {
                CreateViewsVertical();
            }
            else
            {
                CreateViewsHorizontal();
            }
        }

        /// <summary>
        /// Create view for vertical scroller
        /// </summary>
        private void CreateViewsVertical()
        {
            if (_views != null)
            {
                return;
            }

            GameObject clone;
            RectTransform rect;
            var height = 0;
            foreach (var item in _heights.Values)
            {
                height += item;
            }

            var fillCount = 0;
            if (_heights.Count > 0)
            {
                height = height / _heights.Count;
                fillCount = Mathf.RoundToInt(_container.height / height) + 4;
                _views = new GameObject[fillCount];
            }

            for (var i = 0; i < fillCount; i++)
            {
                clone = (GameObject)Instantiate(Prefab, Vector3.zero, Quaternion.identity);
                clone.transform.SetParent(_content);
                clone.transform.localScale = Vector3.one;
                clone.transform.localPosition = Vector3.zero;
                rect = clone.GetComponent<RectTransform>();

                rect.pivot = new Vector2(0.5f, 1f);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = Vector2.one;
                rect.offsetMax = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                _views[i] = clone;
            }

            _rects = new RectTransform[_views.Length];
            for (var i = 0; i < _views.Length; i++)
            {
                _rects[i] = _views[i].gameObject.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Create view for horizontal scroller
        /// </summary>
        private void CreateViewsHorizontal()
        {
            if (_views != null)
            {
                return;
            }

            GameObject clone;
            RectTransform rect;
            var width = 0;
            foreach (var item in _widths.Values)
            {
                width += item;
            }

            width = width / _widths.Count;
            var fillCount = Mathf.RoundToInt(_container.width / width) + 4;
            _views = new GameObject[fillCount];
            for (var i = 0; i < fillCount; i++)
            {
                clone = (GameObject)Instantiate(Prefab, Vector3.zero, Quaternion.identity);
                clone.transform.SetParent(_content);
                clone.transform.localScale = Vector3.one;
                clone.transform.localPosition = Vector3.zero;
                rect = clone.GetComponent<RectTransform>();
                rect.pivot = new Vector2(0f, 0.5f);
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = new Vector2(0f, 1f);
                rect.offsetMax = Vector2.zero;
                rect.offsetMin = Vector2.zero;
                _views[i] = clone;
            }

            _rects = new RectTransform[_views.Length];
            for (var i = 0; i < _views.Length; i++)
            {
                _rects[i] = _views[i].gameObject.GetComponent<RectTransform>();
            }
        }

        /// <summary>
        /// Create labels
        /// </summary>
        private void CreateLabels()
        {
            if (Type == 0)
            {
                CreateLabelsVertical();
            }
            else
            {
                CreateLabelsHorizontal();
            }
        }

        /// <summary>
        /// Create labels for vertical scroller
        /// </summary>
        private void CreateLabelsVertical()
        {
            var topText = new GameObject("TopLabel");
            topText.transform.SetParent(_scroll.viewport.transform);
            TopLabel = topText.AddComponent<TextMeshProUGUI>();
            TopLabel.font = LabelsFont;
            TopLabel.fontSize = 24;
            TopLabel.transform.localScale = Vector3.one;
            TopLabel.alignment = TextAlignmentOptions.Center;
            TopLabel.text = TopPullLabel;
            var rect = TopLabel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = Vector2.one;
            rect.offsetMax = Vector2.zero;
            rect.offsetMin = new Vector2(0f, -LabelOffset);
            rect.anchoredPosition3D = Vector3.zero;
            topText.SetActive(false);
            var bottomText = new GameObject("BottomLabel");
            bottomText.transform.SetParent(_scroll.viewport.transform);
            BottomLabel = bottomText.AddComponent<TextMeshProUGUI>();
            BottomLabel.font = LabelsFont;
            BottomLabel.fontSize = 24;
            BottomLabel.transform.localScale = Vector3.one;
            BottomLabel.alignment = TextAlignmentOptions.Center;
            BottomLabel.text = BottomPullLabel;
            BottomLabel.transform.position = Vector3.zero;
            rect = BottomLabel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(1f, 0f);
            rect.offsetMax = new Vector2(0f, LabelOffset);
            rect.offsetMin = Vector2.zero;
            rect.anchoredPosition3D = Vector3.zero;
            bottomText.SetActive(false);
        }

        /// <summary>
        /// Create labels for horizontal scroller
        /// </summary>
        private void CreateLabelsHorizontal()
        {
            var leftText = new GameObject("LeftLabel");
            leftText.transform.SetParent(_scroll.viewport.transform);
            LeftLabel = leftText.AddComponent<TextMeshProUGUI>();
            LeftLabel.font = LabelsFont;
            LeftLabel.fontSize = 24;
            LeftLabel.transform.localScale = Vector3.one;
            LeftLabel.alignment = TextAlignmentOptions.Center;
            LeftLabel.text = LeftPullLabel;
            var rect = LeftLabel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(0f, 0.5f);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = new Vector2(0f, 1f);
            rect.offsetMax = Vector2.zero;
            rect.offsetMin = new Vector2(-LabelOffset * 2, 0f);
            rect.anchoredPosition3D = Vector3.zero;
            leftText.SetActive(false);
            var rightText = new GameObject("RightLabel");
            rightText.transform.SetParent(_scroll.viewport.transform);
            RightLabel = rightText.AddComponent<TextMeshProUGUI>();
            RightLabel.font = LabelsFont;
            RightLabel.fontSize = 24;
            RightLabel.transform.localScale = Vector3.one;
            RightLabel.alignment = TextAlignmentOptions.Center;
            RightLabel.text = RightPullLabel;
            RightLabel.transform.position = Vector3.zero;
            rect = RightLabel.GetComponent<RectTransform>();
            rect.pivot = new Vector2(1f, 0.5f);
            rect.anchorMin = new Vector2(1f, 0f);
            rect.anchorMax = Vector3.one;
            rect.offsetMax = new Vector2(LabelOffset * 2, 0f);
            rect.offsetMin = Vector2.zero;
            rect.anchoredPosition3D = Vector3.zero;
            rightText.SetActive(false);
        }
    }
}