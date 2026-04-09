using TreeDataStructures.Core;

namespace TreeDataStructures.Implementations.RedBlackTree;

public class RedBlackTree<TKey, TValue> : BinarySearchTreeBase<TKey, TValue, RbNode<TKey, TValue>>
{
    protected override RbNode<TKey, TValue> CreateNode(TKey key, TValue value)
        => new(key, value);

    private bool IsRed(RbNode<TKey, TValue>? node) => node != null && node.Color == RbColor.Red;
    private bool IsBlack(RbNode<TKey, TValue>? node) => node == null || node.Color == RbColor.Black;
    private RbColor GetColor(RbNode<TKey, TValue>? node) => node?.Color ?? RbColor.Black;
    private void SetColor(RbNode<TKey, TValue>? node, RbColor color) { if (node != null) node.Color = color; }
    private void SetRed(RbNode<TKey, TValue>? node) { if (node != null) node.Color = RbColor.Red; }
    private void SetBlack(RbNode<TKey, TValue>? node) { if (node != null) node.Color = RbColor.Black; }

    
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
                
                if (IsRed(uncle))
                {
                    SetBlack(parent);
                    SetBlack(uncle);
                    SetRed(grandParent);
                    node = grandParent;
                    continue;
                }
                
                if (parent.Left == node)
                {
                    RotateRight(grandParent);
                    SetBlack(parent);
                    SetRed(grandParent);
                }
                else
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
        
        if (Root != null) 
        {
            SetBlack((RbNode<TKey, TValue>)Root);
        }
    }
    
    public override bool Remove(TKey key)
    {
        var node = FindNode(key) as RbNode<TKey, TValue>;
        if (node == null)
        {
            return false;
        }

        var deletedColor = node.Color;
        
        bool result = base.Remove(key);
        
        return result;
    }

    protected override void OnNodeRemoved(RbNode<TKey, TValue>? parent, RbNode<TKey, TValue>? child) {}
    
    protected override void RemoveNode(RbNode<TKey, TValue> node)
    {
        RbColor originalColor = node.Color;
        RbNode<TKey, TValue>? replacement = null;
        RbNode<TKey, TValue>? replacementParent = null;
        
        // нет детей или 1 ребенок (1 ребенок может быть только у черной ноды)
        if (node.Left == null)
        {
            replacement = node.Right;
            replacementParent = node.Parent;
            Transplant(node, node.Right);
        }
        else if (node.Right == null)
        {
            replacement = node.Left;
            replacementParent = node.Parent;
            Transplant(node, node.Left);
        }
        else
        {
            // 2 ребенка - ищем замену
            RbNode<TKey, TValue> successor = FindMin(node.Right)!;
            originalColor = successor.Color;
            replacement = successor.Right;
            
            if (successor.Parent == node)
            {
                if (replacement != null)
                {
                    replacement.Parent = successor;
                }
                replacementParent = successor;
            }
            else
            {
                replacementParent = successor.Parent;
                Transplant(successor, successor.Right);
                successor.Right = node.Right;
                if (successor.Right != null)
                {
                    successor.Right.Parent = successor;
                }
            }
            
            Transplant(node, successor);
            successor.Left = node.Left;
            if (successor.Left != null)
            {
                successor.Left.Parent = successor;
            }
            successor.Color = node.Color;
        }
        
        Count--;
        
        if (originalColor == RbColor.Black)
        {
            BalanceAfterRemove(replacement, replacementParent);
        }
        
        if (Root != null)
        {
            SetBlack((RbNode<TKey, TValue>)Root);
        }
    }
    
    /// <summary>
    /// балансировка после удаления черной вершины
    /// node - вершина, которая встала на место удаленной
    /// parent - родитель node (нужен, если node == null)
    /// </summary>
    private void BalanceAfterRemove(RbNode<TKey, TValue>? node, RbNode<TKey, TValue>? parent)
    {
        if (IsRed(node)) //для случая с черной нодой с 1 ребенком
        {
            SetBlack(node);
            return;
        }
        
        // если node == null, начинаем балансировку с родителя
        // иначе node черный, продолжаем
        
        while (node != Root && IsBlack(node))
        {
            // определяем, является ли node левым или правым ребенком
            if (node == parent?.Left)
            {
                var sibling = parent?.Right as RbNode<TKey, TValue>;
                
                // брат красный
                if (IsRed(sibling))
                {
                    SetBlack(sibling);
                    SetRed(parent);
                    RotateLeft(parent!);
                    sibling = parent?.Right as RbNode<TKey, TValue>;
                }
                
                // оба ребенка брата черные
                if (IsBlack(sibling?.Left) && IsBlack(sibling?.Right))
                {
                    SetRed(sibling); 
                    
                    if (IsRed(parent))
                    {
                        SetBlack(parent);
                        return;
                    }
                    else
                    {
                        node = parent;
                        parent = node?.Parent as RbNode<TKey, TValue>;
                    }
                }
                else
                {
                    // левый ребенок брата красный, правый - черный: нужно привести к следующему случаю с правым красным реебенком
                    if (IsBlack(sibling?.Right))
                    {
                        SetBlack(sibling?.Left);
                        SetRed(sibling);
                        RotateRight(sibling!);
                        sibling = parent?.Right as RbNode<TKey, TValue>;
                    }
                    
                    // правый ребенок брата красный
                    SetColor(sibling, GetColor(parent));
                    SetBlack(parent);
                    SetBlack(sibling?.Right);
                    RotateLeft(parent!);
                    node = Root as RbNode<TKey, TValue>;
                    break;
                }
            }
            else
            {
                var sibling = parent?.Left as RbNode<TKey, TValue>;
                
                if (IsRed(sibling))
                {
                    SetBlack(sibling);
                    SetRed(parent);
                    RotateRight(parent!);
                    sibling = parent?.Left as RbNode<TKey, TValue>;
                }
                
                if (IsBlack(sibling?.Right) && IsBlack(sibling?.Left))
                {
                    SetRed(sibling);

                    if (IsRed(parent))
                    {
                        SetBlack(parent);
                        return;
                    }
                    else
                    {
                        node = parent;
                        parent = node?.Parent as RbNode<TKey, TValue>;
                    }
                }
                else
                {
                    if (IsBlack(sibling?.Left))
                    {
                        SetBlack(sibling?.Right);
                        SetRed(sibling);
                        RotateLeft(sibling!);
                        sibling = parent?.Left as RbNode<TKey, TValue>;
                    }
                    
                    SetColor(sibling, GetColor(parent));
                    SetBlack(parent);
                    SetBlack(sibling?.Left);
                    RotateRight(parent!);
                    node = Root as RbNode<TKey, TValue>;
                    break;
                }
            }
        }
        
        if (node != null) //если мы вышли потому что node == root, то надо гарантировать, что корень черный 
        {
            SetBlack(node);
        }
    }

}