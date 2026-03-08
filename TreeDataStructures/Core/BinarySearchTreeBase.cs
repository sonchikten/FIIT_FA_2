using System.Collections;
using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Interfaces;

namespace TreeDataStructures.Core;

public abstract class BinarySearchTreeBase<TKey, TValue, TNode>(IComparer<TKey>? comparer = null) 
    : ITree<TKey, TValue>
    where TNode : Node<TKey, TValue, TNode>
{
    protected TNode? Root;
    public IComparer<TKey> Comparer { get; protected set; } = comparer ?? Comparer<TKey>.Default; // use it to compare Keys

    public int Count { get; protected set; }
    
    public bool IsReadOnly => false;

    public ICollection<TKey> Keys => InOrder().Select(e => e.Key).ToList(); //возвращают все ключи и все значения, преобразует всё в List<TKey>, который реализует ICollection<TKey>
    public ICollection<TValue> Values => InOrder().Select(e => e.Value).ToList();
    
    
    public virtual void Add(TKey key, TValue value)
    {
        if (key == null)
        {
            throw new ArgumentNullException(nameof(key), "Key cannot be null"); //стандартное исключение, которое означает, что ожидалось не null значение, а передали null
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

        if (comparison < 0)
        {
            parent.Left = newNode;
        } 
        else 
        {
            parent.Right = newNode;
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

    private TNode FindMin(TNode node)
    {
        if (node == null)
        {
            throw new ArgumentNullException(nameof(node));
        }

        while (node.Left != null) 
        {
            node = node.Left;
        }
        return node;       
    }
    
    protected virtual void RemoveNode(TNode node)
    {
        if(node.Left == null && node.Right == null)
        {
            TNode? parent = node.Parent;
            Transplant(node, null);
            Count--;
            OnNodeRemoved(parent, null);
            return;
        }

        TNode? parent = node.Parent;
        TNode? child = null;

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
            TNode minRight = FindMin(node.Right);
            TNode original_parent = node.Parent;

            if(minRight.Parent != node)
            {
                Transplant(minRight, minRight.Right); //Transplant(minRight, null)
                minRight.Right = node.Right; //правое поддерево удаляемой ноды становится правым поддеревом новой ноды, которая ставится на место старой
                minRight.Right.Parent = minRight;  
            }

            Transplant(node, minRight);
            minRight.Left = node.Left;
            minRight.Left.Parent = minRight;
            child = minRight.Right;
            Count--;
            OnNodeRemoved(original_parent, minRight);
        }  
    }


    public virtual bool ContainsKey(TKey key) => FindNode(key) != null;
    
    public virtual bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        TNode? node = FindNode(key);
        if (node != null)
        {
            value = node.Value;
            return true;
        }
        value = default;
        return false;
    }

    public TValue this[TKey key]
    {
        get => TryGetValue(key, out TValue? val) ? val : throw new KeyNotFoundException();
        set => Add(key, value);
    }

    
    #region Hooks
    
    /// <summary>
    /// Вызывается после успешной вставки
    /// </summary>
    /// <param name="newNode">Узел, который встал на место</param>
    protected virtual void OnNodeAdded(TNode newNode) { }
    
    /// <summary>
    /// Вызывается после удаления. 
    /// </summary>
    /// <param name="parent">Узел, чей ребенок изменился</param>
    /// <param name="child">Узел, который встал на место удаленного</param>
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
    #endregion
    
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrder() => InOrderTraversal(Root);
    
    private IEnumerable<TreeEntry<TKey, TValue>> InOrderTraversal(TNode? node)
        => new TreeIterator(node, TraversalStrategy.InOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrder() 
        => new TreeIterator(Root, TraversalStrategy.PreOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrder() 
        => new TreeIterator(Root, TraversalStrategy.PostOrder);
    public IEnumerable<TreeEntry<TKey, TValue>>  InOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.InOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PreOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PreOrderReverse);
    public IEnumerable<TreeEntry<TKey, TValue>>  PostOrderReverse() 
        => new TreeIterator(Root, TraversalStrategy.PostOrderReverse);
    
    /// <summary>
    /// Внутренний класс-итератор. 
    /// Реализует паттерн Iterator вручную, без yield return (ban).
    /// </summary>
    private struct TreeIterator : 
        IEnumerable<TreeEntry<TKey, TValue>>, //создавать итераторы
        IEnumerator<TreeEntry<TKey, TValue>> //и есть итератор
    {
        private readonly TNode! _root;
        private readonly TraversalStrategy _strategy; //как обходим

        private TNode? _currentNode;
        private TNode? _nextNode;
        private TreeEntry<TKey, TValue> _current;
        private bool _started;
        private int _currentHeight; 

        
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
            _currentHeight = 0;
        }
        
        
        public bool MoveNext()
        {
            switch (_strategy)
            {
                if (_root == null)
                {
                    return false;
                }

                case TraversalStrategy.PreOrder:
                    return MoveNextPreOrder();
                case TraversalStrategy.InOrder:
                    return MoveNextInOrder();
                case TraversalStrategy.PostOrder:
                    return MoveNextPostOrder();
                    
                case TraversalStrategy.PreOrderReverse:
                    return MoveNextPreOrderReverse();
                case TraversalStrategy.InOrderReverse:
                    return MoveNextInOrderReverse();
                case TraversalStrategy.PostOrderReverse:
                    return MoveNextPostOrderReverse();
                    
                default:
                    throw new NotImplementedException("Strategy not implemented");
            }
        }

        private int GetHeight(TNode? node)
        {
            if (node == null)
            {
                return -1;
            }

            int leftHeight = GetHeight(node.Left);
            int rightHeight = GetHeight(node.Right);

            return 1 + Math.Max(leftHeight, rightHeight);
        }

        private TNode? findLeftmost(TNode? node)
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

        private TNode? findRightmost(TNode? node)
        {
            if (node == null)
            {
                return null;
            }

            while(node.Right != null)
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
            _currentHeight = GetHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentHeight);
            return true;
        }

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
            _currentHeight = GetHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentHeight);
            return true;
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

        private TNode? FindNextPreOrderReverse(TNode? node)
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
            while (parent != null)
            {
                if (parent.Left != null && node != parent.Left)
                {
                    return parent.Left;
                }

                node = parent;
                parent = parent.Parent;
            }

            return null;
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
            _currentHeight = GetHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentHeight);
            return true;
        }

        private bool MoveNextPreOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = _root;
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
            _currentHeight = GetHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentHeight);
            return true;
        }
        
        public void Reset() //возвращает итератор в исходное состояние
        {
            _currentNode = null;
            _nextNode = null;
            _current = default;
            _started = false;
        }

        private TNode? FindFirstPostOrder(TNode? node)
        {
            if (node == null)
            {
                return null;
            }

            while (node.Left != null || node.Right != null)
            {
                if (node.Left != null)
                {
                    node = node.Left;
                }
                else if (node.Right != null)
                {
                    node = node.Right;
                }
            }
            return node;
        }

        private TNode? FindFirstPostOrderReverse(TNode? node)
        {
            if (node == null)
            {
                return null;
            }

            while (node.Left != null || node.Right != null)
            {
                if (node.Right != null)
                {
                    node = node.Right;
                }
                else if (node.Left != null)
                {
                    node = node.Left;
                }
            }
            return node;
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
                node = FindFirstPostOrder(parent.Right);
                return node;
            }

            return parent;
        }

        private TNode? FindNextPostOrderReverse(TNode? node)
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

            if (parent.Right == node && parent.Left != null)
            {
                node = FindFirstPostOrderReverse(parent.Left);
                return node;
            }

            return parent;
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
            if (_nextNode == null)
            {
                return false;
            }
    
            _currentNode = _nextNode;
            _currentHeight = GetHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentHeight);
            return true;
        }

        private bool MoveNextPostOrderReverse()
        {
            if (!_started)
            {
                _started = true;
                _nextNode = FindFirstPostOrderReverse(_root);
            }
            else
            {
                _nextNode = FindNextPostOrderReverse(_currentNode);
            }
            if (_nextNode == null)
            {
                return false;
            }
    
            _currentNode = _nextNode;
            _currentHeight = GetHeight(_currentNode);
            _current = new TreeEntry<TKey, TValue>(_currentNode.Key, _currentNode.Value, _currentHeight);
            return true;
        }

        public void Dispose()
        {
            Reset();
        }
    }
    
    
    private enum TraversalStrategy { InOrder, PreOrder, PostOrder, InOrderReverse, PreOrderReverse, PostOrderReverse }
    
    public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() //для foreach
    {
        return InOrder().Select(e => new KeyValuePair<TKey, TValue>(e.Key, e.Value)).GetEnumerator(); //на вход получает TreeEntry, на выход создает KeyValuePair
    }
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();


    public void Add(KeyValuePair<TKey, TValue> item) => Add(item.Key, item.Value);
    public void Clear() { Root = null; Count = 0; }
    public bool Contains(KeyValuePair<TKey, TValue> item) => ContainsKey(item.Key);
    public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
    {
        ArgumentNullException.ThrowIfNull(array); //проверяем что массив существует
        ArgumentOutOfRangeException.ThrowIfNegative(arrayIndex); //с какого индекса начинать
        if (array.Length - arrayIndex < Count)
        {
            throw new ArgumentException("Not enough space in array");
        }

        foreach (TreeEntry<TKey, TValue> item in InOrder())
        {
            array[arrayIndex++] = new KeyValuePair<TKey, TValue>(item.Key, item.Value);
        }
    }
    public bool Remove(KeyValuePair<TKey, TValue> item) => Remove(item.Key);
}