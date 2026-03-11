using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null)  //abstract - нельзя создавать класс напрямую, только от него можно наследовать
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default;

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList(); //возвращают все ключи и все значения, преобразует всё в List<TKey>, который реализует ICollection<TKey>
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null"); //стандартное исключение, которое означает, что ожидалось не null значение, а передали null, nameof(key) переделывает его в строку 
        }

        if (Root == null)
        {
            Root = CreateNode(key, value);
            OnNodeAdded(Root);
            Count++;
            return;
        }

        TNode? current = Root;
        TNode? parent = null;
        int comparison = 0;

        while(current != null)
        {
            parent = current;
            comparison = Comparer.Compare(key, current.Key);

            if (comparison == 0)  //такой ключ уже существует, меняем значение по этому ключу, т к двух ключей одинаковых быть не может 
            {
                current.Value = value;
                return;
            }

            if (comparison < 0)
            {
                current = current.Left;
            } 
            else 
            {
                current = current.Right;
            }

        }

        TNode newNode = CreateNode(key, value);
        newNode.Parent = parent;

        if (parent != null)
        {
            if (comparison < 0)
            {
                parent.Left = newNode;
            } 
            else 
            {
                parent.Right = newNode;
            }
        }

        Count++;
        OnNodeAdded(newNode);
    }

    
    public virtual bool Remove(TKey key)
    {
        TNode? node = FindNode(key);
        if (node == null) { return false; }

        RemoveNode(node);
        return true;
    }
    
    protected virtual void RemoveNode(TNode node)
    {
        TNode? parent = node.Parent;
        TNode? child = null;

        if(node.Left == null && node.Right == null)
        {
            Transplant(node, null);
            Count--;
            OnNodeRemoved(parent, null);
            return;
        }

        if(node.Left == null) 
        {
            child = node.Right;
            Transplant(node, child);
            Count--;
            OnNodeRemoved(parent, child);
            return;
        }
        else if(node.Right == null)
        {
            child = node.Left;
            Transplant(node, child);
            Count--;
            OnNodeRemoved(parent, child);
            return;
        }
        else
        {
            TNode? minRight = FindMin(node.Right);
            TNode? original_parent = node.Parent;

            if (minRight != null && minRight.Parent != node)
            {
                Transplant(minRight, minRight.Right);
                minRight.Right = node.Right;
                if (minRight.Right != null)
                {
                    minRight.Right.Parent = minRight;
                }
            }

            if (minRight != null)
            {
                Transplant(node, minRight);
                minRight.Left = node.Left;
                if (minRight.Left != null)
                {
                    minRight.Left.Parent = minRight;
                }
                child = minRight.Right;
            }
            Count--;
            OnNodeRemoved(original_parent, minRight);
        }
    }

    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) // out TValue value - выходной парметр, метод модет в него записывать значения,  атрибут [MaybeNullWhen(false)] - для ? рядом с типом данных - value может быть null, но ТОЛЬКО когда метод возвращает false
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default; //0, false, null
        return false;
    }

    public TValue this[TKey key] //позволяет обращаться к дереву как к массиву
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    /// 
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
    /// 
    protected virtual void OnNodeRemoved(TNode? parent, TNode? child) { }
    
    #endregion
    
    
    #region Helpers

    protected abstract TNode CreateNode(TKey key, TValue value);
    
    
    protected TNode? FindNode(TKey key)
    {
        TNode? current = Root;
        while (current != null)
        {
            int cmp = Comparer.Compare(key, current.Key);
            if (cmp == 0) { return current; }
            current = cmp < 0 ? current.Left : current.Right;
        }
        return null;
    }

    protected TNode? FindMin(TNode? node)
    {
        if (node == null)
        {
            return null;
        }

        var current = node;
        while (current.Left != null)
        {
            current = current.Left;
        }
        return current;
    }

    protected TNode? FindMax(TNode? node)
    {
        if (node == null)
        {
            return null;
        }

        var current = node;
        while (current.Right != null)
        {
            current = current.Right;
        }
        return current;
    }

    protected void Transplant(TNode u, TNode? v)
    {
        if (u.Parent == null)
        {
            Root = v;
        }
        else if (u.IsLeftChild)
        {
            u.Parent.Left = v;
        }
        else
        {
            u.Parent.Right = v;
        }
        v?.Parent = u.Parent;
    }

    protected void RotateLeft(TNode x)
    {
        if (x.Right == null)
        {
            throw new InvalidOperationException("Cannot rotate left: right child is null");
        }

        TNode y = x.Right;

        x.Right = y.Left;
        if (y.Left != null)
        {
            y.Left.Parent = x;
        }

        y.Parent = x.Parent;
        if (x.Parent == null)
        {
            Root = y;
        }
        else if (x.IsLeftChild)
        {
            x.Parent.Left = y;
        }
        else 
        {
            x.Parent.Right = y;
        }

        y.Left = x;
        x.Parent = y;
    }

    protected void RotateRight(TNode y)
    {
        if (y.Left == null)
        {
            throw new InvalidOperationException("Cannot rotate right: left child is null");
        }

        TNode x = y.Left;

        y.Left = x.Right;
        if (x.Right != null)
        {
            x.Right.Parent = y;
        }

        x.Parent = y.Parent;
        if (y.Parent == null)
        {
            Root = x;
        }
        else if (y.IsLeftChild)
        {
            y.Parent.Left = x;
        }
        else 
        {
            y.Parent.Right = x;
        }

        x.Right = y;
        y.Parent = x;
    }
    
    protected void RotateBigLeft(TNode x)
    {
        if (x.Right == null)
        {
            throw new InvalidOperationException("Cannot perform big left rotate: right child is null");
        }

        if (x.Right.Left == null)
        {
            throw new InvalidOperationException("Cannot perform big left rotate: right child has not left child");
        }

        RotateRight(x.Right);
        RotateLeft(x);
    }
    
    protected void RotateBigRight(TNode y)
    {
        if (y.Left == null)
        {
            throw new InvalidOperationException("Cannot perform big right rotate: left child is null");
        }

        if (y.Left.Right == null)
        {
            throw new InvalidOperationException("Cannot perform big right rotate: left child has not right child");
        }

        RotateLeft(y.Left);
        RotateRight(y);
    }
    
    protected void RotateDoubleLeft(TNode x)
    {
        if (x.Right == null)
        {
            throw new InvalidOperationException("Cannot perform double left rotate: right child is null");
        }

        if (x.Right.Right == null)
        {
            throw new InvalidOperationException("Cannot perform double left rotate: right child has not right child");
        }

        TNode? second_node = x.Right;
        RotateLeft(x);
        RotateLeft(second_node);
    }
    
    protected void RotateDoubleRight(TNode y)
    {
        if (y.Left == null)
        {
            throw new InvalidOperationException("Cannot perform double right rotate: left child is null");
        }

        if(y.Left.Left == null)
        {
            throw new InvalidOperationException("Cannot perform double right rotate: left child has not left child");
        }

        TNode? second_node = y.Left;
        RotateRight(y);
        RotateRight(second_node);
    }

    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>> InOrder() => new TreeIterator(Root, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrder() => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrder() => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>> InOrderReverse() => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PreOrderReverse() => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>> PostOrderReverse() => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    private struct TreeIterator : IEnumerable<TreeEntry<TKey, TValue>>, IEnumerator<TreeEntry<TKey, TValue>>
    {
        private readonly TNode? _root;
        private readonly TraversalStrategy _strategy;
        private TNode? _currentNode;
        private TNode? _nextNode;
        private TreeEntry<TKey, TValue> _current;
        private bool _started;

        public IEnumerator<TreeEntry<TKey, TValue>> GetEnumerator() => this;
        IEnumerator IEnumerable.GetEnumerator() => this;
        public TreeEntry<TKey, TValue> Current => _current;
        object IEnumerator.Current => Current;

        public TreeIterator(TNode? root, TraversalStrategy strategy)
        {
            _root = root;
            _strategy = strategy;
            _currentNode = null;
            _nextNode = null;
            _current = default;
            _started = false;
        }

        public bool MoveNext()
        {
            if (_root == null) return false;

            switch (_strategy)
            {
                case TraversalStrategy.InOrder: return MoveNextInOrder();
                case TraversalStrategy.PreOrder: return MoveNextPreOrder();
                case TraversalStrategy.PostOrder: return MoveNextPostOrder();
                case TraversalStrategy.InOrderReverse: return MoveNextInOrderReverse();
                case TraversalStrategy.PreOrderReverse: return MoveNextPreOrderReverse();
                case TraversalStrategy.PostOrderReverse: return MoveNextPostOrderReverse();
                default: throw new NotImplementedException();
            }
        }

        #region Traversal Helpers

        private int GetNodeHeight(TNode? node)
        {
            if (node == null) 
            {
                return -1;
            }
            
            int leftHeight = GetNodeHeight(node.Left);
            int rightHeight = GetNodeHeight(node.Right);
            
            return Math.Max(leftHeight, rightHeight) + 1;
        }

        private TNode? FindLeftmost(TNode? node)
        {
            if (node == null)
            {
                return null;
            }

            while (node.Left != null)
            {
                node = node.Left;
            }

            return node;
        }

        private TNode? FindRightmost(TNode? node)
        {
            if (node == null)
            {
                return null;
            }
            while (node.Right != null)
            {
                node = node.Right;
            }

            return node;
        }

        private TNode? FindNextInOrder(TNode? node)
        {
            if (node == null) 
            {
                return null;
            }

            if (node.Right != null)
            {
                return FindLeftmost(node.Right);
            }

            TNode? parent = node.Parent;
            while (parent != null && node == parent.Right)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        private TNode? FindPrevInOrder(TNode? node)
        {
            if (node == null) 
            {
                return null;
            }

            if (node.Left != null)
            {
                return FindRightmost(node.Left);
            } 

            TNode? parent = node.Parent;
            while (parent != null && node == parent.Left)
            {
                node = parent;
                parent = parent.Parent;
            }
            return parent;
        }

        private TNode? FindNextPreOrder(TNode? node)
        {
            if (node == null) 
            {
                return null;
            }

            if (node.Left != null)
            {
                return node.Left;
            }

            if (node.Right != null)
            {
                return node.Right;
            }  

            TNode? parent = node.Parent;
            while (parent != null)
            {
                if (parent.Right != null && node != parent.Right)
                {
                    return parent.Right;
                }
                node = parent;
                parent = parent.Parent;
            }
            return null;
        }

        private TNode? FindFirstPostOrder(TNode? node)
        {
            if (node == null) 
            {
                return null;
            }

            var current = node;
            while (current.Left != null || current.Right != null)
            {
                if (current.Left != null)
                {
                    current = current.Left;
                }
                else
                {
                    current = current.Right!; //! - знаем, что Right не null
                }
            }
            return current;
        }

        private TNode? FindNextPostOrder(TNode? node)
        {
            if (node == null) 
            {
                return null;
            }

            TNode? parent = node.Parent;
            if (parent == null) 
            {
                return null;
            }

            if (parent.Left == node && parent.Right != null)
            {    
                return FindFirstPostOrder(parent.Right);
            }

            return parent;
        }

        private TNode? FindFirstPreOrderReverse(TNode? node)
        {
            if (node == null) 
            {
                return null;
            }

            var current = node;
            while (current.Right != null || current.Left != null)
            {
                if (current.Right != null)
                {
                    current = current.Right;
                }
                else if (current.Left != null)
                {
                    current = current.Left;
                }
            }
            return current;
        }

        private TNode? FindNextPreOrderReverse(TNode? node)
        {
            if (node == null) 
            {
                return null;
            }

            TNode? parent = node.Parent;
            if (parent == null) 
            {
                return null;
            }

            if (parent.Right == node)
            {
                if (parent.Left != null)
                {
                    var temp = parent.Left;
                    while (temp != null && (temp.Right != null || temp.Left != null))
                    {
                        if (temp.Right != null)
                        {
                            temp = temp.Right;
                        }
                        else if (temp.Left != null)
                        {
                            temp = temp.Left;
                        }
                    }
                    return temp;
                }
                return parent;
            }
            
            return parent;
        }

        private TNode? FindNextPostOrderReverse(TNode? node)
        {
            if (node == null)
            {
                return null;
            }

            if (node.Right != null)
            {
                return node.Right;
            }

            if (node.Left != null)
            {
                return node.Left;
            }

            TNode? parent = node.Parent;
            TNode? current = node;
            
            while (parent != null)
            {
                if (parent.Right == current)
                {
                    if (parent.Left != null)
                    {
                        return parent.Left;
                    }

                    current = parent;
                    parent = parent.Parent;
                    continue;
                }
                
                if (parent.Left == current)
                {
                    current = parent;
                    parent = parent.Parent;
                    continue;
                }
            }
            
            return null;
        }

        #endregion

        #region Standard Traversals

        private bool MoveNextInOrder()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = FindLeftmost(_root);
            }
            else
            {
                _nextNode = FindNextInOrder(_currentNode);
            }

            if (_nextNode == null) 
            {
                return false;
            }

            _currentNode = _nextNode;
            int height = GetNodeHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, height);
            return true;
        }

        private bool MoveNextPreOrder()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = _root;
            }
            else
            {
                _nextNode = FindNextPreOrder(_currentNode);
            }

            if (_nextNode == null) 
            {
                return false;
            }

            _currentNode = _nextNode;
            int height = GetNodeHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, height);
            return true;
        }

        private bool MoveNextPostOrder()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = FindFirstPostOrder(_root);
            }
            else
            {
                _nextNode = FindNextPostOrder(_currentNode);
            }

            if (_nextNode == null) return false;

            _currentNode = _nextNode;
            int height = GetNodeHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, height);
            return true;
        }

        #endregion

        #region Reverse Traversals

        private bool MoveNextInOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = FindRightmost(_root);
            }
            else
            {
                _nextNode = FindPrevInOrder(_currentNode);
            }

            if (_nextNode == null)
            {
                return false;
            }

            _currentNode = _nextNode;
            int height = GetNodeHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, height);
            return true;
        }

        private bool MoveNextPreOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = FindFirstPreOrderReverse(_root);
            }
            else
            {
                _nextNode = FindNextPreOrderReverse(_currentNode);
            }

            if (_nextNode == null) 
            {
                return false;
            }

            _currentNode = _nextNode;
            int height = GetNodeHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, height);
            return true;
        }

        private bool MoveNextPostOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = _root;
            }
            else
            {
                _nextNode = FindNextPostOrderReverse(_currentNode);
            }

            if (_nextNode == null) return false;

            _currentNode = _nextNode;
            int height = GetNodeHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, height);
            return true;
        }

        #endregion

        public void Reset()
        {
            _currentNode = null;
            _nextNode = null;
            _current = default;
            _started = false;
        }

        public void Dispose() => Reset();
    }
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() //обходчик, если просто вызывать его без уточнения, какой стратегией проходить 
    {
        return InOrder().Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value)).GetEnumerator();
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) //копирует все элементы дерева в массив с указанного индекса
    {
        if (array == null)
        {
            throw new ArgumentNullException(nameof(array), "Array can't be null");
        }

        if (arrayIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(arrayIndex), "Index can't be negative");
        }

        int available = array.Length - arrayIndex;
        if (available < Count)
        {
            throw new ArgumentException($"There is not available space. Need {Count} places, there are {available}");
        }

        int currentIndex = arrayIndex;

        foreach (TreeEntry<TKey, TValue> item in InOrder())
        {
            array[currentIndex] = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
            currentIndex++;
        }
    }
    
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}