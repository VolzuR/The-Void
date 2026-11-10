using System.Collections.Generic;
using UnityEngine;

public class PlayerInventory : MonoBehaviour
{
    public List<Package> packages = new List<Package>();

    public void AddPackage(Package p) { packages.Add(p); }
    public void Remove(Package p) { packages.Remove(p); }
    public Package FindFor(string destinationId)
    {
        return packages.Find(p => p.destinationId == destinationId);
    }
}