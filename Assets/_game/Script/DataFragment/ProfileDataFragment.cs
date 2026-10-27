using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable, CreateAssetMenu(
    menuName = "Stuff/Data Fragment/Profile Fragment",
    fileName = "Profile Fragment")]
public class ProfileDataFragment : DataFragment
{
    public static ProfileDataFragment cur;
    public Sprite[] spr_avatars;
    public Sprite[] spr_frames;

    public Data gameData;

    [NonSerialized]
    public List<string> names = new List<string>
    {
    "Juan", "Maria", "Carlos", "Sofia", "Miguel",
    "Alejandro", "Ana", "Lucia", "Pablo", "Carmen",
    "Javier", "Sara", "Manuel", "Laura", "Antonio",
    "Beatriz", "Francisco", "Elena", "Fernando", "Alicia",

    "Michael", "Emily", "Christopher", "Jessica", "Joshua",
    "Amanda", "Daniel", "Jennifer", "James", "Ashley",
    "Matthew", "Sarah", "Andrew", "Elizabeth", "David",
    "Samantha", "Justin", "Megan", "Ryan", "Lauren",

    "Jean", "Marie", "Pierre", "Claire", "Jacques",
    "Sophie", "Louis", "Isabelle", "Paul", "Amelie",
    "Philippe", "Camille", "Bernard", "Charlotte", "Alain",
    "Lucie", "Georges", "Pauline", "Andre", "Leo",

    "Jose", "Isabel", "Mario", "Victoria", "Alberto",
    "Oliver", "Amelia", "Henry", "Samuel", "Julie",
    "Luc", "Nathalie", "Victor", "Lukas", "Emma",

    "Maximilian", "Sophia", "Felix", "Marco", "Giulia",
    "Luca", "Francesca", "Giorgio", "Pedro", "Ines",
    "Joao", "Tiago", "Arjun", "Aishwarya", "Raj",
    "Priya", "Vikram", "Gabriel", "Rafael", "Fernanda",

    "Bruna", "Thiago", "Putra", "Dewi", "Wayan",
    "Agung", "Rani", "Angel", "Ricardo", "Chinedu",
    "Ngozi", "Ifeanyi", "Adebayo", "Chioma", "Santiago",

    "Luisa", "Valeria", "Piotr", "Anna", "Krzysztof",
    "Ewa", "Marek", "Ola", "Solveig", "Kjetil",
    "Ingrid", "Eirik", "Mikko", "Aino", "Janne",

    "Liisa", "Petri", "Ahmet", "Fatma", "Mehmet",
    "Zeynep", "Can", "Ali", "Fatemeh", "Reza",
    "Hossein", "Mwangi", "Amina", "Omondi", "Wanjiru",

    "Mutua", "Mateo", "Florencia", "Joaquin", "Camila",
    "Ignacio", "Benjamin", "Valentina", "Ionut", "Stefan",
    "Andreea", "Mihai", "Abdul", "Ayesha", "Mohammad",

    "Zara", "Hassan", "Miriam", "Yosef", "Noa",
    "Avraham", "Abebe", "Alem", "Bekele", "Desta",
    "Tigist", "Aroha", "Tane", "Hemi", "Rangi",

    "Mahuika", "Sibusiso", "Thandeka", "Lerato", "Mfundo",
    "Bongani", "Nikolas", "Katerina", "Yannis", "Eleni",
    "Aoife", "Conor", "Niamh", "Oisin", "Roisin",

    "Catarina", "Istvan", "Zoltan", "Agnes", "Laszlo",
    "Judit", "Milo", "Jelena", "Vuk", "Dragana",
    "Ivan", "Marko", "Petra", "Georgi", "Ivanka",

    "Radoslav", "Stoyan", "Desislava", "Nino", "Giorgi",
    "Tamar", "Levan", "Elene", "Mansur", "Dilbar",
    "Ulugbek", "Shirin", "Javlon", "Andres", "Gabriela",

    "Luis", "Patricia", "Agustina", "Rodrigo", "Diego",
    "Martin", "Elle", "Niillas", "Ailu", "Risten",
    "Oskari", "Karim", "Layla", "Tarek", "Nadia",

    "Samir", "Bikram", "Sita", "Prakash", "Laxmi",
    "Manoj", "Chenda", "Sophea", "Piseth", "Dara",
    "Sothy", "Sven", "Astrid", "Kari", "Frederik",

    "Sofie", "Jens", "Lars", "Juhani", "Emilia",
    "Tapio", "Veera", "Matti", "Lucas", "Lotte",
    "Maarten", "Annelies", "Svenja", "Petr", "Jana",

    "Vaclav", "Lenka", "Karel", "Zuzana", "Juraj",
    "Tatiana", "Michal", "Max", "Chantal", "Tom",
    "Colette", "Henri", "Arben", "Ilir", "Ariana",

    "Luan", "Edi", "Goran", "Ivana", "Nikola",
    "Amir", "Selma", "Eldar", "Alma", "Denis",
    "Hayk", "Mariam", "Vardan", "Arpine", "Narek",

    "Rasim", "Zahra", "Elchin", "Leyla", "Togrul",
    "Paola", "Hernan", "Felipe", "Sebastian", "Keshawn",
    "Anya", "Shane", "Janelle", "Imran", "Viliame",

    "Losalini", "Mosese", "Adi", "Eroni", "Kaloris",
    "Selina", "Moses", "Rosina", "Petro", "Sione",
    "Mele", "Lopeti", "Feki"
    };

#if UNITY_EDITOR
    private void OnEnable()
    {
        cur = this;
    }
#else
    private void Awake()
    {
        cur = this;
    }
#endif

    public override void Load()
    {
        if (!LoadData(ref gameData, key))
        {
            ResetData();
        }

        // ==============================
        // FIX DATA CŨ
        // ==============================

        if (gameData == null)
        {
            ResetData();
        }

        bool needSave = false;

        // User cũ chưa có username
        if (string.IsNullOrWhiteSpace(gameData.userName))
        {
            gameData.userName = GetRandomName();
            needSave = true;
        }

        // User cũ chưa có joinTime
        if (gameData.joinTime == default ||
            gameData.joinTime.Year <= 1)
        {
            gameData.joinTime = DateTime.Now;
            needSave = true;
        }

        // Chống index avatar lỗi nếu data cũ
        if (spr_avatars != null && spr_avatars.Length > 0)
        {
            if (gameData.idAvatar < 0 ||
                gameData.idAvatar >= spr_avatars.Length)
            {
                gameData.idAvatar = 0;
                needSave = true;
            }
        }

        // Chống index frame lỗi
        if (spr_frames != null && spr_frames.Length > 0)
        {
            if (gameData.idFrame < 0 ||
                gameData.idFrame >= spr_frames.Length)
            {
                gameData.idFrame = 0;
                needSave = true;
            }
        }

        // Chống index lựa chọn avatar lỗi
        if (gameData.idChoose < 0)
        {
            gameData.idChoose = 0;
            needSave = true;
        }

        if (needSave)
        {
            Save();
        }
    }

    public override void Save()
    {
        SaveData(gameData, key);
    }

    public override void ResetData()
    {
        gameData = new Data
        {
            idChoose = 0,
            idAvatar = 0,
            idFrame = 0,

            // Tạo tên ngay từ đầu
            userName = GetRandomName(),

            // Lưu ngày bắt đầu chơi
            joinTime = DateTime.Now
        };

        Save();
    }

    private string GetRandomName()
    {
        if (names != null && names.Count > 0)
        {
            return names[UnityEngine.Random.Range(0, names.Count)];
        }

        return "Player";
    }

    public (Sprite, Sprite) GetAvatarAndFrame(
        int avatarIndex,
        int frameIndex)
    {
        return (
            spr_avatars[avatarIndex],
            spr_frames[frameIndex]
        );
    }

    public void AddWinStreak()
    {
        gameData.winStreak++;

        gameData.winStreakMax = Mathf.Max(
            gameData.winStreakMax,
            gameData.winStreak
        );

        Save();
    }

    public void AddFirstTryWin()
    {
        gameData.firstTryWin++;

        gameData.firstTryWinMax = Mathf.Max(
            gameData.firstTryWinMax,
            gameData.firstTryWin
        );

        Save();
    }

    public void ResetFirstTryWin()
    {
        if (gameData.firstTryWin == 0)
            return;

        gameData.firstTryWin = 0;
        Save();
    }

    public void ResetWinStreak()
    {
        if (gameData.winStreak == 0)
            return;

        gameData.winStreak = 0;
        Save();
    }

    public void ResetAllProfileStats()
    {
        gameData.firstTryWin = 0;
        gameData.firstTryWinMax = 0;

        gameData.winStreak = 0;
        gameData.winStreakMax = 0;

        Save();
    }

    public void RecordLevelWin(bool isFirstTry)
    {
        // Mỗi lần thắng đều tăng Win Streak.
        gameData.winStreak++;

        gameData.winStreakMax = Mathf.Max(
            gameData.winStreakMax,
            gameData.winStreak
        );

        if (isFirstTry)
        {
            gameData.firstTryWin++;

            gameData.firstTryWinMax = Mathf.Max(
                gameData.firstTryWinMax,
                gameData.firstTryWin
            );
        }
        else
        {
            // Thắng nhưng đã từng thua/Retry level này.
            gameData.firstTryWin = 0;
        }

        Save();
    }

    public void RecordLevelLose()
    {
        // Thua sẽ ngắt cả hai chuỗi.
        gameData.winStreak = 0;
        gameData.firstTryWin = 0;

        Save();
    }

    [Serializable]
    public class Data
    {
        public int idChoose;
        public int idAvatar;
        public int idFrame;

        public string userName;
        public DateTime joinTime;

        public int firstTryWin = 0;
        public int firstTryWinMax = 0;
        public int winStreak = 0;
        public int winStreakMax = 0;
    }
}