using System.Diagnostics.CodeAnalysis;
using TreeDataStructures.Implementations.BST;

namespace TreeDataStructures.Implementations.Splay;

public class SplayTree<TKey, TValue> : BinarySearchTree<TKey, TValue>
    where TKey : IComparable<TKey>
{
    protected override BstNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private void Splay(BstNode<TKey, TValue> node)
    {
        while (node.Parent != null) //пока узел не станет корнем
        {
            var parent = node.Parent;
            var grandParent = parent.Parent;

            if (grandParent == null) //родитель - корень
            {
                if (parent.Left == node)
                {
                    RotateRight(parent);
                }
                else
                {
                    RotateLeft(parent);
                }
            }
            else if (parent.Left == node && grandParent.Left == parent)
            {
                RotateDoubleRight(grandParent);
            }
            else if (parent.Right == node && grandParent.Right == parent)
            {
                RotateDoubleLeft(grandParent);
            }
            else if (parent.Left == node && grandParent.Right == parent)
            {
                RotateBigLeft(grandParent);
            }
            else
            {
                RotateBigRight(grandParent);
            }
        }
    }
    
    protected override void OnNodeAdded(BstNode<TKey, TValue> newNode)
    {
        Splay(newNode);
    }
    
    protected override void OnNodeRemoved(BstNode<TKey, TValue>? parent, BstNode<TKey, TValue>? child)
    {
        if (parent != null)
            Splay(parent);
        else if (child != null)
            Splay(child);
    }
    
    public override bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            value = node.Value;
            return true;
        }

        value = default;
        return false;
    }

    public override bool ContainsKey(TKey key)
    {
        var node = FindNode(key);
        if (node != null)
        {
            Splay(node);
            return true;
        }
        return false;
    }
    
}
