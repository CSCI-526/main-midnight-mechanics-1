using UnityEngine;

namespace Game.Skills
{
    /// <summary>固定 8 个乐器技能，只存实现引用。</summary>
    [CreateAssetMenu(menuName = "Game/Skills/Skill Library (Slim)")]
    public class SkillLibrary : ScriptableObject
    {
        public enum ActiveSkillId
        {
            Drum = 0,
            Bass = 1,
            ElectricGuitar = 2,
            Keyboard = 3,
            Vocal = 4,
            Synth = 5,
            Trumpet = 6,
            AcousticGuitar = 7
        }

        [Header("Implementations (length = 8, 顺序与枚举一致)")]
        [SerializeField] private ActiveSkillBase[] implementations = new ActiveSkillBase[8];

        public ActiveSkillBase GetImpl(ActiveSkillId id)
        {
            int i = (int)id;
            if (implementations != null && i >= 0 && i < implementations.Length)
                return implementations[i];
            return null;
        }

        public bool TryGetIdByImpl(ActiveSkillBase impl, out ActiveSkillId id)
        {
            id = default;
            if (!impl || implementations == null) return false;
            for (int i = 0; i < implementations.Length; i++)
            {
                if (implementations[i] == impl)
                {
                    id = (ActiveSkillId)i;
                    return true;
                }
            }
            return false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (implementations == null || implementations.Length != 8)
            {
                var old = implementations;
                implementations = new ActiveSkillBase[8];
                if (old != null)
                {
                    for (int i = 0; i < Mathf.Min(8, old.Length); i++)
                        implementations[i] = old[i];
                }
            }
        }
#endif
    }
}