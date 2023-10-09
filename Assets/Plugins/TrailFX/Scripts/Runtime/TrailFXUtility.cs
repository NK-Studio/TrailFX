using UnityEngine;
namespace NKStudio
{
    public static class TrailFXUtility
    {
        /// <summary>
        /// ������ ����ΰ�?
        /// </summary>
        public static bool IsEditorMode => !Application.isPlaying;

        /// <summary>
        /// �÷��� ����ΰ�?
        /// </summary>
        public static bool IsPlayMode => Application.isPlaying;
    }
}
