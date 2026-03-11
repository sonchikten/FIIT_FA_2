using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.AVL;

public class AvlTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, AvlNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override AvlNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private int GetHeight(AvlNode<TKey, TValue>? node)
    {
        return node?.Height ?? 0; //если левая часть null вернет 0 
    }

    private void UpdateHeight(AvlNode<TKey, TValue> node)
    {
        if (node == null)
        {
            return;
        }

        node.Height = 1 + Math.Max(GetHeight(node.Left), GetHeight(node.Right));
    }

    private int GetBalanceFactor(AvlNode<TKey, TValue>? node)
        => node == null ? 0 : GetHeight(node.Left) - GetHeight(node.Right);
    
    private void BalanceNode(AvlNode<TKey, TValue> node)
    {
        UpdateHeight(node);
        int balance = GetBalanceFactor(node);

        if (balance > 1 && GetBalanceFactor(node.Left) >= 0)
        {
            RotateRight(node);
            UpdateHeight(node);
            UpdateHeight(node.Left!);
            UpdateHeight(node.Right!);
        }
        else if (balance < -1 && GetBalanceFactor(node.Right) <= 0)
        {
            RotateLeft(node);
            UpdateHeight(node);
            UpdateHeight(node.Left!);
            UpdateHeight(node.Right!);
        }
        else if (balance > 1 && GetBalanceFactor(node.Left) < 0)
        {
            RotateBigRight(node);
            UpdateHeight(node);
            UpdateHeight(node.Left!);
            UpdateHeight(node.Right!);
        }
        else if (balance < -1 && GetBalanceFactor(node.Right) > 0)
        {
            RotateBigLeft(node);
            UpdateHeight(node);
            UpdateHeight(node.Left!);
            UpdateHeight(node.Right!);
        }
    }

    protected override void OnNodeAdded(AvlNode<TKey, TValue> newNode)
    {
        var current = newNode;
        
        while (current != null)
        {
            BalanceNode(current);
            current = current.Parent as AvlNode<TKey, TValue>; //безопасное приведение типов из Node<TKey, TValue, TNode>, вернет null, если не получится
        }
    }

    protected override void OnNodeRemoved(AvlNode<TKey, TValue>? parent, AvlNode<TKey, TValue>? child)
    {
        var current = parent;
        
        while (current != null)
        {
            BalanceNode(current);
            current = current.Parent as AvlNode<TKey, TValue>;
        }
    }
}