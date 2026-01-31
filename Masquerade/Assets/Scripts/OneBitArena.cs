using System.Collections.Generic;
using UnityEngine;

public class OneBitArena : MonoBehaviour
{
    [SerializeField] int num_waves;

    [SerializeField] List<GameObject> wave_one = new List<GameObject>();

    [SerializeField] List<GameObject> wave_two = new List<GameObject>();

    [SerializeField] List<GameObject> wave_three = new List<GameObject>();

    [SerializeField] List<GameObject> wave_four = new List<GameObject>();

    [SerializeField] List<GameObject> wave_five = new List<GameObject>();

    [SerializeField] List<GameObject> exits = new List<GameObject>();

    [SerializeField] List<GameObject> spawn_points = new List<GameObject>();

    [SerializeField] List<BasicAI> living_enemies = new List<BasicAI>();

    [SerializeField] bool telephoneArena;

    [SerializeField] bool FinalArea;

    [SerializeField] List<GameObject> visible_arena_blocks = new List<GameObject>();

    [SerializeField] List<GameObject> invisible_arena_blocks = new List<GameObject>();

    [SerializeField] List<OneBitArena> nearby_arenas;

    bool arena_complete;

    int wave_num;

    bool player_in_arena;

    private void Awake()
    {
        UIEvents.RecievePlayerDeath += player_death;
    }

    private void OnDestroy()
    {
        UIEvents.RecievePlayerDeath -= player_death;
    }

    // Update is called once per frame
    void Update()
    {
        if (living_enemies.Count > 0)
        {
            for (int i = 0; i < living_enemies.Count; i++)
            {
                if (living_enemies[i] != null)
                {
                    if (living_enemies[i].dead)
                    {
                        living_enemies.Remove(living_enemies[i]);
                        if (living_enemies.Count <= 0)
                        {
                            next_wave();
                        }
                    }
                }
                else
                {
                    living_enemies.Remove(living_enemies[i]);
                    if (living_enemies.Count <= 0)
                    {
                        next_wave();
                    }


                }
            }

        }




    }

    public void SpawnEnemies(List<GameObject> enemies_to_spawn)
    {
        living_enemies.Clear();
        List<GameObject> availible_spawns = new List<GameObject>(spawn_points);


        for (int i = 0; i < enemies_to_spawn.Count; i++)
        {
            if (enemies_to_spawn[i] != null)
            {
                Debug.Log(availible_spawns.Count);
                GameObject spawn_location = availible_spawns[Random.Range(0, availible_spawns.Count - 1)];
                availible_spawns.Remove(spawn_location);
                GameObject new_enemy = Instantiate(enemies_to_spawn[i], spawn_location.transform.position, spawn_location.transform.rotation);
                living_enemies.Add(new_enemy.GetComponentInChildren<BasicAI>());
            }
        }

    }

    public void DespawnEnemies()
    {
        for (int i = 0; i < living_enemies.Count; ++i)
        {
            Destroy(living_enemies[i].gameObject);
        }
        living_enemies.Clear();
    }

    private void next_wave()
    {
        if (wave_num < num_waves)
        {
            wave_num++;
            switch (wave_num)
            {
                case 1:
                    SpawnEnemies(wave_one);
                    break;
                case 2:
                    SpawnEnemies(wave_two);
                    break;
                case 3:
                    SpawnEnemies(wave_three);
                    break;
                case 4:
                    SpawnEnemies(wave_four);
                    break;
                case 5:
                    SpawnEnemies(wave_five);
                    break;
            }
        }
        else
        {
            arena_complete = true;
            GameManager.Instance.SwitchMusic(false);
            foreach (GameObject exit in exits)
            {
                exit.SetActive(false);
            }
            if (telephoneArena)
            {
                GameManager.Instance.quest_progress();
            }

        }
    }

    private void player_death()
    {
        GameManager.Instance.SwitchMusic(false);
        wave_num = 0;
        player_in_arena = false;
        DespawnEnemies();
        foreach (GameObject exit in exits)
        {
            exit.gameObject.SetActive(false);
        }
    }

    private void OnTriggerEnter(Collider other)
    {


        if (other.CompareTag("GameController"))
        {
            foreach (GameObject arena_block in visible_arena_blocks)
            {
                if (!arena_block.activeInHierarchy)
                {
                    arena_block.SetActive(true);
                }
            }
            foreach (GameObject arena_block in invisible_arena_blocks)
            {
                if (arena_block.activeInHierarchy)
                {
                    arena_block.SetActive(false);
                }
            }

            if (!player_in_arena && !arena_complete)
            {
                player_in_arena = true;
                foreach (GameObject exit in exits)
                {
                    exit.gameObject.SetActive(true);
                }
                GameManager.Instance.SwitchMusic(true);
                wave_num = 0;
                next_wave();
            }



        }
    }
}
