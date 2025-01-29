using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

public class GameObjectCreationTest
{
    [UnityTest]
    public IEnumerator GameObjectIsCreated()
    {
        var gameObject = new GameObject("TestObject");

        yield return null;

        Assert.IsNotNull(gameObject);
    }
}
