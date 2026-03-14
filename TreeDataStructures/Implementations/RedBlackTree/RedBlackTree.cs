using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
    where TKey : IComparable<TKey>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private RbColor? _deletedColor;
    private int _childCount;
    
    private bool IsRed(RbNode<TKey, TValue>? node) => node != null && node.Color == RbColor.Red;
    private bool IsBlack(RbNode<TKey, TValue>? node) => node == null || node.Color == RbColor.Black;

    private RbColor GetColor(RbNode<TKey, TValue>? node) => node?.Color ?? RbColor.Black; //тк все null-листья считаются черными

    private void SetColor(RbNode<TKey, TValue>? node, RbColor color)
    {
        if (node == null) return;
        node.Color = color;
    }

    private void SetRed(RbNode<TKey, TValue>? node)
    {
        if (node == null) return; 
        node.Color = RbColor.Red;
    }
    
    private void SetBlack(RbNode<TKey, TValue>? node)
    {
        if (node == null) return;
        node.Color = RbColor.Black;
    }
    
    protected override void OnNodeAdded(RbNode<TKey, TValue> newNode)
    {
        var node = newNode;
    
        while (node != null && IsRed(node))
        {
            var parent = node.Parent as RbNode<TKey, TValue>;
            if (parent == null) break;
            
            var grandParent = parent.Parent as RbNode<TKey, TValue>;
            
            if (IsBlack(parent)) break;
            
            if (grandParent != null && grandParent.Left == parent)
            {
                var uncle = grandParent.Right as RbNode<TKey, TValue>;
                
                if (IsRed(uncle)) //дядя красный, родитель красный
                {
                    SetBlack(parent);
                    SetBlack(uncle);
                    SetRed(grandParent);
                    
                    node = grandParent;
                    continue;
                }
                
                if (parent.Left == node) //дяди нет (null - черный), родитель красный
                {
                    RotateRight(grandParent);
                    
                    SetBlack(parent);
                    SetRed(grandParent);
                }
                else //дяди нет (null - черный), правый ребенок красного родителя
                {
                    RotateBigRight(grandParent);
                    
                    SetBlack(node);
                    SetRed(grandParent);
                }   
                break;
            }
            else
            {
                var uncle = grandParent!.Left as RbNode<TKey, TValue>;
                
                if (IsRed(uncle))
                {
                    SetBlack(parent);
                    SetBlack(uncle);
                    SetRed(grandParent);
                    node = grandParent;
                    continue;
                }
                
                if (parent.Right == node)
                {
                    RotateLeft(grandParent);
                    SetBlack(parent);
                    SetRed(grandParent);
                }
                else
                {
                    RotateBigLeft(grandParent);
                    SetBlack(node);
                    SetRed(grandParent);
                }
                
                break;
            }
        }
        
        if (Root != null) SetBlack((RbNode<TKey, TValue>)Root);
    }

    public override bool Remove(TKey key)
    {
        var node = FindNode(key) as RbNode<TKey, TValue>;
        if (node == null) return false;

        _deletedColor = node.Color;
        _childCount = (node.Left != null ? 1 : 0) + (node.Right != null ? 1 : 0);
        
        return base.Remove(key);
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child)
    {
        if (_deletedColor == RbColor.Red)
        {
            return;
        }

        if (_childCount == 1 && child != null && IsRed(child)) // удаление черной ноды с 1 ребенком (по сути может быть только красный ребенок)
        {
            SetBlack(child);
            return; 
        }

        if (_childCount == 0 && parent != null)
        {
            bool wasLeft = parent.Left == null;
            BalanceAfterRemove(null, parent, wasLeft);
        }

        if (Root != null) SetBlack((RbNode<TKey, TValue>)Root);
    }

    private void BalanceAfterRemove(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent = null, bool? wasLeft = null)
    {
        if (node != null)
        {
            parent = node.Parent as RbNode<TKey, TValue>;
            if (parent == null) return;
            wasLeft = parent.Left == node;
        }
        else if (parent == null || wasLeft == null) return;

        while (true)
        {
            var sibling = wasLeft == true
                ? parent.Right as RbNode<TKey, TValue>
                : parent.Left as RbNode<TKey, TValue>;

            if (sibling == null) //поднимаемся вверх, потому что нет брата, нечем балансировать
            {
                if (node != null)
                {
                    node = parent;
                    parent = node.Parent as RbNode<TKey, TValue>;
                    if (parent == null) break;
                    wasLeft = parent.Left == node;
                }
                else
                {
                    wasLeft = parent.Parent?.Left == parent;
                    parent = parent.Parent as RbNode<TKey, TValue>;
                    if (parent == null) break;
                }
                continue;
            }

            if (IsRed(sibling)) //брат красный и попадаем вероятно куда-то в кейсы с черным братом
            {
                SetBlack(sibling);
                SetRed(parent);

                if (wasLeft == true)
                {
                    RotateLeft(parent);
                }
                else
                {
                    RotateRight(parent);
                }

                sibling = wasLeft == true
                    ? parent.Right as RbNode<TKey, TValue>
                    : parent.Left as RbNode<TKey, TValue>;
                
                continue;
            }

            var leftNephew = sibling?.Left as RbNode<TKey, TValue>;
            var rightNephew = sibling?.Right as RbNode<TKey, TValue>;

            if (IsBlack(leftNephew) && IsBlack(rightNephew)) //оба племянника - черные
            {
                SetRed(sibling);

                if (IsRed(parent))
                {
                    SetBlack(parent);  // родитель был красный - красим в черный
                    return;
                }

                // родитель черный - поднимаемся выше
                if (node != null)
                {
                    node = parent;
                    parent = node.Parent as RbNode<TKey, TValue>;
                    if (parent == null) break;
                    wasLeft = parent.Left == node;
                }
                else
                {
                    wasLeft = parent.Parent?.Left == parent;
                    parent = parent.Parent as RbNode<TKey, TValue>;
                    if (parent == null) break;
                }
                continue;
            }

            if (wasLeft == true)
            {
                if (IsBlack(rightNephew) && IsRed(leftNephew)) //дальний правый племянник черный, ближний - красный, отсюда вероятно попадем в следующий кейс
                {
                    SetBlack(leftNephew);
                    SetRed(sibling);
                    RotateRight(sibling!);
                    sibling = parent.Right as RbNode<TKey, TValue>;
                    rightNephew = sibling?.Right as RbNode<TKey, TValue>;
                }

                if (IsRed(rightNephew)) //правый племянник красный, левый любой (оба красные, левый черный, правый красный)
                {
                    SetColor(sibling, GetColor(parent));
                    SetBlack(parent);
                    SetBlack(rightNephew);
                    RotateLeft(parent);
                    return;
                }
            }
            else
            {
                if (IsBlack(leftNephew) && IsRed(rightNephew))
                {
                    SetBlack(rightNephew);
                    SetRed(sibling);
                    RotateLeft(sibling!);
                    sibling = parent.Left as RbNode<TKey, TValue>;
                    leftNephew = sibling?.Left as RbNode<TKey, TValue>;
                }
                
                if (IsRed(leftNephew))
                {
                    SetColor(sibling, GetColor(parent));
                    SetBlack(parent);
                    SetBlack(leftNephew);
                    RotateRight(parent);
                    return;
                }
            }
        }

        if (node != null) SetBlack(node);
    }
}