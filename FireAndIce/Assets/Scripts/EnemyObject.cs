using UnityEngine;


public enum ObjectType
{
    Weapon,
    Projectile
}
public class EnemyObject : MonoBehaviour
{
    public int damage;
    public Enemy parent;

    public ObjectType objectType;


}
