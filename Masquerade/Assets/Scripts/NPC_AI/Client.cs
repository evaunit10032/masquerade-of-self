using System.Collections.Generic;
using UnityEngine;

public class Client : MonoBehaviour
{
    public float show_hostile;
    public float health;

    public List<Client> spotted_clients = new List<Client>();

    public bool is_player;
}
