using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class GameObjectActiveTest
{
    [UnityTest]
    public IEnumerator GameObjectIsActive()
    {
        var gameObject = new GameObject("TestObject");
        gameObject.SetActive(false);
        yield return null;
        Assert.IsFalse(gameObject.activeSelf);
    }
}
