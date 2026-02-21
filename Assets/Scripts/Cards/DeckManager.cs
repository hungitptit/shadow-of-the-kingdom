using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the shared draw pile and discard pile.
/// Singleton accessed via DeckManager.Instance.
/// </summary>
public class DeckManager : MonoBehaviour
{
    public static DeckManager Instance;

    [Header("Card Assets (assign all CardData ScriptableObjects)")]
    public List<CardData> allCards;

    private List<CardData> drawPile  = new();
    private List<CardData> discardPile = new();

    // Card back sprite for the deck UI
    public Sprite cardBackSprite;

    void Awake()
    {
        Instance = this;
    }

    // ── Setup ─────────────────────────────────────────────────────

    public void BuildDeck()
    {
        drawPile.Clear();
        discardPile.Clear();

        foreach (CardData card in allCards)
        {
            for (int i = 0; i < card.count; i++)
                drawPile.Add(card);
        }

        Shuffle(drawPile);
        Debug.Log($"[Deck] Built deck with {drawPile.Count} cards.");
    }

    // ── Draw ──────────────────────────────────────────────────────

    /// <summary>Draw one card. Returns null if deck is empty after reshuffling.</summary>
    public CardData DrawOne()
    {
        if (drawPile.Count == 0)
        {
            if (discardPile.Count == 0) return null;
            ReshuffleDiscard();
        }
        if (drawPile.Count == 0) return null;

        CardData card = drawPile[0];
        drawPile.RemoveAt(0);
        return card;
    }

    /// <summary>Peek at the top N cards without removing them.</summary>
    public List<CardData> PeekTop(int count)
    {
        List<CardData> result = new();
        for (int i = 0; i < Mathf.Min(count, drawPile.Count); i++)
            result.Add(drawPile[i]);
        return result;
    }

    // ── Discard ───────────────────────────────────────────────────

    public void Discard(CardData card)
    {
        if (card != null)
            discardPile.Add(card);
    }

    public void DiscardAll(List<CardData> cards)
    {
        foreach (var c in cards) Discard(c);
        cards.Clear();
    }

    // ── Player hand helpers ───────────────────────────────────────

    /// <summary>
    /// Give player one card from the top of the deck.
    /// Enforces max hand size of 5 — returns false if hand is full.
    /// </summary>
    public bool DealTo(Player player)
    {
        if (player.hand.Count >= Player.MaxHandSize) return false;
        CardData card = DrawOne();
        if (card == null) return false;
        player.hand.Add(card);
        return true;
    }

    /// <summary>Deal starting hand of 3 cards to player.</summary>
    public void DealStartingHand(Player player)
    {
        for (int i = 0; i < 3; i++)
            DealTo(player);
    }

    /// <summary>
    /// Player plays a card from hand. Removes it and puts in discard
    /// unless it's an Item (which stays equipped).
    /// </summary>
    public bool PlayCard(Player player, CardData card)
    {
        if (!player.hand.Contains(card)) return false;
        player.hand.Remove(card);

        if (card.cardType != CardType.Item && card.cardType != CardType.HiddenAction)
            Discard(card);
        // Items go to player.equippedItems — handled by CardEffectExecutor
        // HiddenActions go to target.hiddenActionsOnMe — handled by GameManager

        return true;
    }

    public int DrawPileCount  => drawPile.Count;
    public int DiscardPileCount => discardPile.Count;

    // ── Internals ─────────────────────────────────────────────────

    void ReshuffleDiscard()
    {
        drawPile.AddRange(discardPile);
        discardPile.Clear();
        Shuffle(drawPile);
        Debug.Log("[Deck] Reshuffled discard into draw pile.");
    }

    void Shuffle(List<CardData> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
