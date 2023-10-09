using UnityEngine;
namespace NKStudio
{
    public static class TrailFXUtility
    {
        /// <summary>
        /// 에디터 모드인가?
        /// </summary>
        public static bool IsEditorMode => !Application.isPlaying;

        /// <summary>
        /// 플레이 모드인가?
        /// </summary>
        public static bool IsPlayMode => Application.isPlaying;
    }
}
