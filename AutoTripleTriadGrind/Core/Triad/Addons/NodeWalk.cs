using System;
using System.Runtime.InteropServices;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace AutoTripleTriadGrind.Core.Triad.Addons;

internal static class NodeWalk
{
    private const int MaxSiblings = 64;

    private const int MaxDepth = 24;

    private const int ComponentNodeTypeStart = 1000;

    public unsafe static bool IsVisible(AtkResNode* node) => node != null && node->IsVisible();

    public unsafe static int ChildCount(AtkResNode* node)
    {
        if (node == null || node->ChildNode == null)
        {
            return 0;
        }
        var count = 1;
        var sibling = node->ChildNode->PrevSiblingNode;
        while (sibling != null && count < MaxSiblings)
        {
            count++;
            sibling = sibling->PrevSiblingNode;
        }
        return count;
    }

    public unsafe static AtkResNode* Child(AtkResNode* node, int index, int expectedCount)
    {
        if (ChildCount(node) != expectedCount || index >= expectedCount)
        {
            return null;
        }
        var child = node->ChildNode;
        for (var i = 0; i < index; i++)
        {
            child = child->PrevSiblingNode;
        }
        return child;
    }

    public unsafe static AtkResNode* ComponentChild(AtkResNode* maybeComponent, int index, int expectedCount)
    {
        if (maybeComponent == null || (int)maybeComponent->Type < ComponentNodeTypeStart)
        {
            return null;
        }
        return ComponentChild(((AtkComponentNode*)maybeComponent)->Component, index, expectedCount);
    }

    public unsafe static AtkResNode* ComponentChild(AtkComponentBase* component, int index, int expectedCount)
    {
        if (component == null || component->UldManager.NodeListCount != expectedCount || index >= expectedCount)
        {
            return null;
        }
        return component->UldManager.NodeList[index];
    }

    public unsafe static string? Text(AtkResNode* node)
    {
        if (node == null || node->Type != NodeType.Text)
        {
            return null;
        }
        return Marshal.PtrToStringUTF8((nint)((AtkTextNode*)node)->NodeText.StringPtr.Value);
    }

    public unsafe static string? TexturePath(AtkResNode* node)
    {
        if (node == null || node->Type != NodeType.Image)
        {
            return null;
        }
        var image = (AtkImageNode*)node;
        if (image->PartsList == null || image->PartId > image->PartsList->PartCount)
        {
            return null;
        }
        var uldAsset = image->PartsList->Parts[(int)image->PartId].UldAsset;
        if (uldAsset == null || uldAsset->AtkTexture.TextureType != TextureType.Resource || uldAsset->AtkTexture.Resource == null)
        {
            return null;
        }
        var textureHandle = uldAsset->AtkTexture.Resource->TexFileResourceHandle;
        if (textureHandle != null)
        {
            return textureHandle->ResourceHandle.FileName.ToString();
        }
        return null;
    }

    public unsafe static bool AnyVisibleText(AtkResNode* node, Func<string, bool> matches, int depth = 0)
    {
        if (depth > MaxDepth || !IsVisible(node))
        {
            return false;
        }
        if (node->Type == NodeType.Text && Text(node) is { } text && matches(text))
        {
            return true;
        }
        if (node->ChildNode == null)
        {
            return false;
        }
        var visited = 0;
        var child = node->ChildNode;
        while (child != null && visited < MaxSiblings)
        {
            visited++;
            if (AnyVisibleText(child, matches, depth + 1))
            {
                return true;
            }
            child = child->PrevSiblingNode;
        }
        return false;
    }
}
