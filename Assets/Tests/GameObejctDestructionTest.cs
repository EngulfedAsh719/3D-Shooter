using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class GameObjectDestructionTest
{
    [UnityTest]
    public IEnumerator GameObjectIsDestroyed()
    {
        var gameObject = new GameObject("TestObject");
        Object.Destroy(gameObject);
        yield return null;
        Assert.IsTrue(gameObject == null);
    }
}
