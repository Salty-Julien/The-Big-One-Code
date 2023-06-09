using Steamworks;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoomPool : MonoBehaviour
{
    [SerializeField] string[] miscsList;
    [SerializeField] string[] roomList;
    [SerializeField] string[] npcList;
    [SerializeField] List<string> currentRoomList = new();

    GameObject currentRoom;
    CatchReferences references;

    int roomsRemaining = 2; //startRoom + boss
    int numberOfRoomsCleared;

    private void Awake()
    {
        references = FindObjectOfType<CatchReferences>();
    }

    private void Start()
    {
        BuildSector();
    }

    private void BuildSector()
    {
        int numberOfRooms = UnityEngine.Random.Range(60, 70);
        numberOfRooms = Mathf.CeilToInt(numberOfRooms * references.GetSimulationsPlaceHolder().GetIncreaseSectorWidth());
        
        int numberOfNPCRooms = UnityEngine.Random.Range(5, 9);
        
        roomsRemaining += numberOfRooms + numberOfNPCRooms;

        for (int i = 0; i < numberOfRooms; i++)
        {
            int index = UnityEngine.Random.Range(0, roomList.Length);

            currentRoomList.Add(roomList[index]);
        }

        currentRoomList.Insert(0, miscsList[0]);

        if (references.GetPlayerStatistics().GetUnlockSleepRoom())
        {
            currentRoomList.Insert(Mathf.RoundToInt(numberOfRooms + numberOfNPCRooms) / 2, miscsList[1]);

            roomsRemaining++;
        }

        currentRoomList.Add(miscsList[2]);

        int percentageToHaveNPCRoomFirst = UnityEngine.Random.Range(0, 2);
        bool haveNPCRoomFirst = false;

        if (percentageToHaveNPCRoomFirst == 0)
        {
            bool roomFinded = false;

            while (!roomFinded)
            {
                int index = UnityEngine.Random.Range(0, npcList.Length);

                if (npcList[index] == "NPC Room - Blacksmith") continue;
                if (npcList[index] == "NPC Room - Throne") continue;

                roomFinded = true;

                currentRoomList.Insert(1, npcList[index]);

                haveNPCRoomFirst = true;
            }
        }

        int roomsBeforeNPCRoom = (numberOfRooms + 2) / numberOfNPCRooms;

        for (int i = 0; i < numberOfNPCRooms; i++)
        {
            bool roomFinded = false;

            if (haveNPCRoomFirst)
            {
                i++;

                haveNPCRoomFirst = false;
            }

            while (!roomFinded)
            {
                int index = UnityEngine.Random.Range(0, npcList.Length);

                if (npcList[index] == "NPC Room - Throne") continue;

                int numberOfSameRoom = 0;

                foreach (string room in currentRoomList)
                {
                    if (room == "NPC Room - Casino" && npcList[index] == "NPC Room - Casino")
                    {
                        numberOfSameRoom = 2;

                        break;
                    }

                    if (room == npcList[index])
                    {
                        numberOfSameRoom++;
                    }
                }

                if (numberOfSameRoom <= 1)
                {
                    roomFinded = true;

                    currentRoomList.Insert(roomsBeforeNPCRoom * (i + 1), npcList[index]);
                }
            }
        }

        currentRoomList.Insert(1, npcList[8]);

        references.GetSector().SetRoomsRemaining(roomsRemaining);

        NextRoom();
    }

    public void NextRoom()
    {
        if (currentRoom)
        {
            DestroyLastRoom();
        }

        if (references.GetPlayerController().GetCursed() || references.GetSector().GetRoomsCleared() < 1)
        {
            SpawnNextRoom();
        }
        else
        {
            InfinitSector();
        }

        references.GetPlayerHealth().HealBetweenRoom();

        references.GetPassiveObject().LetTheMusicPlay();
        references.GetPassiveObject().CactusSeed();
        references.GetPassiveObject().HeartSeed();
        references.GetPassiveObject().ResetPurification();

        references.GetRoomCount().AddRoom();

        references.GetGameManager().IncreaseNumberOfRoomClear();

        CheckAchievement();
    }

    private void CheckAchievement()
    {
        if (references.GetGameManager().GetNoobMode()) return;

        if (SteamManager.Initialized)
        {
            if (!references.GetPlayerController().GetCursed() && numberOfRoomsCleared == 98)
            {
                SteamUserStats.GetAchievement("ACH_100_ROOM_INFINIT_SECTOR", out bool achievementUnlock);

                if (!achievementUnlock)
                {
                    references.GetAchievements().SetAchievement("ACH_100_ROOM_INFINIT_SECTOR");

                    SteamUserStats.SetAchievement("ACH_100_ROOM_INFINIT_SECTOR");
                }
            }

            int numberOfRoomClear = references.GetGameManager().GetNumberOfRoomClear();

            if (numberOfRoomClear == 1000)
            {
                SteamUserStats.GetAchievement("ACH_CLEARED_ROOMS", out bool achievementUnlock);
                
                if (achievementUnlock)
                {
                    references.GetAchievements().SetAchievement("ACH_CLEARED_ROOMS");

                    SteamUserStats.SetAchievement("ACH_CLEARED_ROOMS");
                }
            }

            SteamUserStats.StoreStats();
        }
    }

    private void SpawnNextRoom()
    {
        string roomPath;

        if (currentRoomList[0].Contains("NPC"))
        {
            roomPath = "NPCs/" + currentRoomList[0];
        }
        else if (currentRoomList[0].Contains("Miscs"))
        {
            roomPath = "Miscs/" + currentRoomList[0];
        }
        else
        {
            roomPath = "Hostiles/" + currentRoomList[0];
        }

        GameObject room = Resources.Load<GameObject>(roomPath);
        currentRoom = Instantiate(room, transform);

        currentRoomList.RemoveAt(0);
    }

    private void InfinitSector()
    {
        GameObject roomPrefab;

        numberOfRoomsCleared++;

        if (numberOfRoomsCleared == 15 || numberOfRoomsCleared == 30)
        {
            int pnjIndex = UnityEngine.Random.Range(0, npcList.Length);

            GameObject room = Resources.Load<GameObject>("NPCs/" + npcList[pnjIndex]);
            roomPrefab = Instantiate(room, transform);

            if (numberOfRoomsCleared == 30)
            {
                numberOfRoomsCleared = 0;
            }
        }
        else
        {
            int roomIndex = UnityEngine.Random.Range(0, roomList.Length);

            GameObject room = Resources.Load<GameObject>("Hostiles/" + roomList[roomIndex]);
            roomPrefab = Instantiate(room, transform);
        }

        currentRoom = roomPrefab;
    }

    private void DestroyLastRoom()
    {
        references.GetEnemyPool().ClearList();
        references.GetSector().DecreaseRoomsRemaining(1, true);

        foreach (TextInfo item in FindObjectsOfType<TextInfo>())
        {
            Destroy(item.gameObject);
        }

        foreach (Projectile item in FindObjectsOfType<Projectile>())
        {
            Destroy(item.gameObject);
        }

        references.GetPlayerController().LavaVFX(false);
        
        Destroy(currentRoom);
    }

    public void AddRoomToSector(int numberOfRooms)
    {
        for (int i = 0; i < numberOfRooms; i++)
        {
            int index = UnityEngine.Random.Range(0, roomList.Length);

            currentRoomList.Insert(currentRoomList.Count - 1, roomList[index]);
        }
    }

    public void RemoveSectorRooms(int numberOfRooms)
    {
        for (int i = 0; i < numberOfRooms; i++)
        {
            if (currentRoomList.Count - 2 > 0)
            {
                currentRoomList.RemoveAt(currentRoomList.Count - 2);
            }
        }
    }

    public void NextRoomNPC()
    {
        int index = UnityEngine.Random.Range(0, npcList.Length);

        currentRoomList.Insert(0, npcList[index]);
    }

    public string GetNextRoomName()
    {
        if (currentRoomList.Count > 0)
        {
            return currentRoomList[0];
        }
        else
        {
            return "";
        }
    }

    public Room GetCurrentRoom()
    {
        return currentRoom.GetComponent<Room>();
    }
}
