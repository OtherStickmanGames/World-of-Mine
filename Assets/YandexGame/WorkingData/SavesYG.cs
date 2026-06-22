
using UnityEngine;

namespace YG
{
    [System.Serializable]
    public class SavesYG
    {
        // "Техн�ческ�е сохранен�я" для работы плаг�на (Не удалять)
        public int idSave;
        public bool isFirstSession = true;
        public string language = "ru";
        public bool promptDone;

        // Тестовые сохранен�я для демо сцены
        // Можно удал�ть этот код, но тогда удал�те � демо (папка Example)


        // Ваш� сохранен�я

        //public UserData userData;
        public string nickname;
        public bool tutorialComplete;
        public bool tutorialSkiped;
        public Vector3 position;
        
        
        internal int money;
        public string newPlayerName;
        public bool[] openLevels = new bool[3];

        // Поля (сохранен�я) можно удалять � создавать новые. Пр� обновлен�� �гры сохранен�я ломаться не должны


        // Вы можете выполн�ть как�е то действ�я пр� загрузке сохранен�й
        public SavesYG()
        {
            // Допуст�м, задать значен�я по умолчан�ю для отдельных элементов масс�ва
        }
    }
}
