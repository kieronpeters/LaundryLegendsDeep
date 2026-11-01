using System;
using UnityEngine;

namespace LaundryLegends.Arcade
{
    /// <summary>What the player asked the fighter to do.</summary>
    public enum FightAction
    {
        MoveLeft,
        MoveRight,
        Punch,
        Kick,
        Block
    }

    /// <summary>How a finished round ended.</summary>
    public enum FightOutcome
    {
        InProgress,
        PlayerWon,
        PlayerLost,
        Draw
    }

    /// <summary>
    /// A small versus fighting game: two fighters on a line, punches and kicks in
    /// range, a guard that softens damage, a round clock and a knockout. Rules live
    /// here and are deterministic, so the cabinet only feeds input and draws state.
    /// <para>
    /// Original fighters and rules; no third-party characters or branding.
    /// </para>
    /// </summary>
    public sealed class FightGame
    {
        public const float MinSeparation = 0.12f;
        public const int MaxHealth = 100;
        public const float RoundSeconds = 60f;

        private const float MoveSpeed = 0.42f;
        private const float PunchRange = 0.24f;
        private const float KickRange = 0.30f;
        private const float PunchDamage = 6f;
        private const float KickDamage = 10f;
        private const float PunchWindup = 0.10f;
        private const float KickWindup = 0.18f;
        private const float Recovery = 0.28f;
        private const float GuardSeconds = 0.35f;

        private readonly System.Random _random;
        private float _playerTimer;
        private float _opponentTimer;
        private float _playerGuard;
        private float _opponentGuard;
        private float _aiTimer;
        private float _roundTimer = RoundSeconds;

        public FightGame(int seed = 0)
        {
            _random = seed == 0 ? new System.Random() : new System.Random(seed);
            PlayerX = 0.38f;
            OpponentX = 0.62f;
            PlayerHealth = MaxHealth;
            OpponentHealth = MaxHealth;
        }

        public float PlayerX { get; private set; }

        public float OpponentX { get; private set; }

        public int PlayerHealth { get; private set; }

        public int OpponentHealth { get; private set; }

        public float TimeRemaining => Mathf.Max(0f, _roundTimer);

        public FightOutcome Outcome { get; private set; } = FightOutcome.InProgress;

        public bool IsOver => Outcome != FightOutcome.InProgress;

        public bool PlayerGuarding => _playerGuard > 0f;

        public bool OpponentGuarding => _opponentGuard > 0f;

        public bool PlayerAttacking => _playerTimer > 0f;

        public bool OpponentAttacking => _opponentTimer > 0f;

        public float Distance => Mathf.Abs(OpponentX - PlayerX);

        /// <summary>Starts a fresh round, keeping nothing from the last one.</summary>
        public void Restart()
        {
            PlayerX = 0.38f;
            OpponentX = 0.62f;
            PlayerHealth = MaxHealth;
            OpponentHealth = MaxHealth;
            _playerTimer = 0f;
            _opponentTimer = 0f;
            _playerGuard = 0f;
            _opponentGuard = 0f;
            _aiTimer = 0f;
            _roundTimer = RoundSeconds;
            Outcome = FightOutcome.InProgress;
        }

        /// <summary>
        /// Applies one input. Returns true only when the action was accepted, so the
        /// cabinet can ignore input during the post-attack recovery.
        /// </summary>
        public bool TryAction(FightAction action)
        {
            if (IsOver)
            {
                return false;
            }

            switch (action)
            {
                case FightAction.MoveLeft:
                    return Move(-1f);
                case FightAction.MoveRight:
                    return Move(1f);
                case FightAction.Block:
                    _playerGuard = GuardSeconds;
                    return true;
                case FightAction.Punch:
                    return Attack(PunchDamage, PunchWindup, PunchRange);
                case FightAction.Kick:
                    return Attack(KickDamage, KickWindup, KickRange);
                default:
                    return false;
            }
        }

        /// <summary>Advances the round. Public so tests can run a whole match quickly.</summary>
        public void Tick(float deltaSeconds)
        {
            if (IsOver)
            {
                return;
            }

            float step = Mathf.Max(0f, deltaSeconds);
            _roundTimer -= step;
            float guardDecay = step;
            _playerGuard = Mathf.Max(0f, _playerGuard - guardDecay);
            _opponentGuard = Mathf.Max(0f, _opponentGuard - guardDecay);

            if (_playerTimer > 0f)
            {
                _playerTimer -= step;
                if (_playerTimer <= 0f)
                {
                    ResolvePlayerAttack();
                }
            }

            if (_opponentTimer > 0f)
            {
                _opponentTimer -= step;
                if (_opponentTimer <= 0f)
                {
                    ResolveOpponentAttack();
                }
            }

            Think(step);

            if (_roundTimer <= 0f)
            {
                _roundTimer = 0f;
                SettleOnTime();
            }
        }

        private void Think(float step)
        {
            _aiTimer -= step;
            if (_aiTimer > 0f || _opponentTimer > 0f)
            {
                return;
            }

            _aiTimer = 0.55f + (float)_random.NextDouble() * 0.4f;
            float distance = Distance;

            if (PlayerAttacking && distance <= PunchRange && _random.NextDouble() < 0.5)
            {
                _opponentGuard = GuardSeconds;
                return;
            }

            if (distance > KickRange)
            {
                float direction = OpponentX > PlayerX ? -1f : 1f;
                MoveOpponent(direction * MoveSpeed * _aiTimer);
                return;
            }

            double roll = _random.NextDouble();
            if (roll < 0.35)
            {
                _opponentTimer = KickWindup;
            }
            else if (roll < 0.65)
            {
                _opponentTimer = PunchWindup;
            }
            else
            {
                _opponentGuard = GuardSeconds;
            }
        }

        private bool Move(float direction)
        {
            float next = Mathf.Clamp(PlayerX + direction * MoveSpeed * TimeStep, 0.05f, 0.95f);
            if (Mathf.Abs(next - OpponentX) < MinSeparation)
            {
                next = OpponentX - Mathf.Sign(OpponentX - PlayerX) * MinSeparation;
            }

            PlayerX = Mathf.Clamp(next, 0.05f, 0.95f);
            return true;
        }

        private void MoveOpponent(float delta)
        {
            float next = Mathf.Clamp(OpponentX + delta, 0.05f, 0.95f);
            if (Mathf.Abs(next - PlayerX) < MinSeparation)
            {
                next = PlayerX + Mathf.Sign(OpponentX - PlayerX) * MinSeparation;
            }

            OpponentX = Mathf.Clamp(next, 0.05f, 0.95f);
        }

        /// <summary>Movement is applied in equal steps; 0.016 keeps a normal frame.</summary>
        private const float TimeStep = 0.016f;

        private bool Attack(float damage, float windup, float range)
        {
            if (_playerTimer > 0f)
            {
                return false;
            }

            if (Distance > range)
            {
                return false;
            }

            _playerTimer = windup;
            PendingDamage = damage;
            return true;
        }

        private float PendingDamage { get; set; }

        private void ResolvePlayerAttack()
        {
            if (Distance > KickRange)
            {
                return;
            }

            float damage = PendingDamage * (_opponentGuard > 0f ? 0.3f : 1f);
            OpponentHealth = Mathf.Max(0, OpponentHealth - Mathf.RoundToInt(damage));
            CheckKnockout();
        }

        private void ResolveOpponentAttack()
        {
            if (Distance > KickRange)
            {
                return;
            }

            float damage = KickDamage * (_playerGuard > 0f ? 0.3f : 1f);
            PlayerHealth = Mathf.Max(0, PlayerHealth - Mathf.RoundToInt(damage));
            CheckKnockout();
        }

        private void CheckKnockout()
        {
            if (OpponentHealth <= 0)
            {
                Outcome = FightOutcome.PlayerWon;
            }
            else if (PlayerHealth <= 0)
            {
                Outcome = FightOutcome.PlayerLost;
            }
        }

        private void SettleOnTime()
        {
            if (PlayerHealth > OpponentHealth)
            {
                Outcome = FightOutcome.PlayerWon;
            }
            else if (OpponentHealth > PlayerHealth)
            {
                Outcome = FightOutcome.PlayerLost;
            }
            else
            {
                Outcome = FightOutcome.Draw;
            }
        }
    }
}
