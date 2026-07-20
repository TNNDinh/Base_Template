using System;
using System.Collections.Generic;
using UnityEngine;

namespace Ezg.Feature.Gameplay.Battle
{
    /// <summary>
    ///     Engine bộ bài 1 trận (THUẦN LOGIC — test được không cần scene; view móc qua callback).
    ///     Vòng đời: <see cref="StartBattle" /> (xáo deck + bốc bài mở màn) → mỗi player round gọi
    ///     <see cref="StartTurn" /> (nạp energy theo round + bốc 1 lá) → người chơi <see cref="TryPlay" /> tiêu energy.
    ///     Hết draw pile thì xáo lại discard. Tay đầy mà bốc thêm → lá bốc bị ĐỐT (vào discard).
    ///     <para>Energy theo <see cref="CardEnergy" /> (tăng dần mỗi round, có trần) — hero giữ bài qua các round.</para>
    /// </summary>
    public class CardDeckRuntime
    {
        public const int HandLimit = 6;   // tối đa lá trên tay
        public const int OpeningHand = 3; // số lá bốc lúc mở màn
        public const int DrawPerTurn = 1; // số lá bốc đầu mỗi player round

        private readonly List<string> _draw = new List<string>();
        private readonly List<string> _hand = new List<string>();
        private readonly List<string> _discard = new List<string>();

        private int _playerRound;

        /// <param name="deckCardIds">Danh sách id thẻ (được lặp) tạo thành bộ bài.</param>
        public CardDeckRuntime(IList<string> deckCardIds)
        {
            if (deckCardIds != null)
                for (int i = 0; i < deckCardIds.Count; i++)
                    if (!string.IsNullOrEmpty(deckCardIds[i]))
                        _draw.Add(deckCardIds[i]);
        }

        /// <summary>Energy còn lại của lượt hiện tại.</summary>
        public int Energy { get; private set; }

        /// <summary>Player round hiện tại (1-based) — quyết định energy đầy của lượt.</summary>
        public int PlayerRound => _playerRound;

        public IReadOnlyList<string> Hand => _hand;
        public int DrawCount => _draw.Count;
        public int DiscardCount => _discard.Count;
        public int HandCount => _hand.Count;

        /// <summary>Tay đổi (view: dựng lại các lá bài).</summary>
        public Action OnHandChanged;
        /// <summary>Energy đổi (view: cập nhật thanh energy).</summary>
        public Action OnEnergyChanged;

        /// <summary>Vào trận: xáo bộ bài + bốc bài mở màn. Gọi 1 lần.</summary>
        public void StartBattle()
        {
            Shuffle(_draw);
            DrawN(OpeningHand);
            OnHandChanged?.Invoke();
        }

        /// <summary>Đầu 1 player round: nạp energy theo round + bốc bài đầu lượt.</summary>
        public void StartTurn(int playerRound)
        {
            _playerRound = Mathf.Max(1, playerRound);
            Energy = CardEnergy.ForRound(_playerRound);
            OnEnergyChanged?.Invoke();
            DrawN(DrawPerTurn);
            OnHandChanged?.Invoke();
        }

        /// <summary>Đánh 1 lá trên tay: đủ energy → trừ energy, chuyển lá vào discard. Trả về true nếu đánh được.</summary>
        public bool TryPlay(string cardId, int cost)
        {
            int idx = _hand.IndexOf(cardId);
            if (idx < 0 || cost > Energy) return false;
            _hand.RemoveAt(idx);
            _discard.Add(cardId);
            Energy -= cost;
            OnEnergyChanged?.Invoke();
            OnHandChanged?.Invoke();
            return true;
        }

        /// <summary>Rút thêm <paramref name="n" /> lá (thẻ DrawCards). Trả về số lá thực rút lên tay.</summary>
        public int DrawExtra(int n)
        {
            int drawn = DrawN(n);
            if (drawn > 0) OnHandChanged?.Invoke();
            return drawn;
        }

        /// <summary>+<paramref name="n" /> energy lượt này (thẻ GainEnergy), không quá trần.</summary>
        public void GainEnergy(int n)
        {
            if (n <= 0) return;
            Energy = Mathf.Min(CardEnergy.Cap, Energy + n);
            OnEnergyChanged?.Invoke();
        }

        // ----- Nội bộ -----

        /// <summary>Bốc tối đa <paramref name="n" /> lá; hết draw pile thì xáo discard vào. Tay đầy → đốt lá bốc.</summary>
        private int DrawN(int n)
        {
            int drawn = 0;
            for (int i = 0; i < n; i++)
            {
                if (_draw.Count == 0) ReshuffleDiscard();
                if (_draw.Count == 0) break; // hết sạch bài
                var card = _draw[_draw.Count - 1];
                _draw.RemoveAt(_draw.Count - 1);
                if (_hand.Count >= HandLimit) { _discard.Add(card); continue; } // tay đầy → đốt
                _hand.Add(card);
                drawn++;
            }

            return drawn;
        }

        private void ReshuffleDiscard()
        {
            if (_discard.Count == 0) return;
            _draw.AddRange(_discard);
            _discard.Clear();
            Shuffle(_draw);
        }

        /// <summary>Fisher–Yates dùng UnityEngine.Random (test: gọi Random.InitState trước để tất định).</summary>
        private static void Shuffle(List<string> list)
        {
            for (int i = list.Count - 1; i > 0; i--)
            {
                int j = UnityEngine.Random.Range(0, i + 1);
                (list[i], list[j]) = (list[j], list[i]);
            }
        }
    }
}
