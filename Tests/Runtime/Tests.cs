using System;
using UnityEngine;

namespace Framework.Testing
{
    public static class Tests
    {
        public static void DoTest()
        {
            KActionTest();
            PersistentDataManagerUnitTest();
        }

        class TestPersitent
        {
            public int data1;
            public float data2;
            public Vector2 data3;
            public Vector3 data4;
            public string data5;
        }

        class
            TestData
        {

            public int intValue;
            public float floatValue;
            public string strValue;
        }

        static void PersistentDataManagerUnitTest()
        {
            // ����������ݽṹ

            string testKey = "unit_test.data";

            // 1. ��������
            var data = new TestData
            {
                intValue = 42,
                floatValue = 3.14f,
                strValue = "Hello"
            };
            PersistentDataManager.Instance.SaveData(testKey, data);

            // 2. �������ݲ�����
            var loaded = PersistentDataManager.Instance.LoadData<TestData>(testKey);
            Debug.Assert(loaded != null, "Loaded data should not be null");
            Debug.Assert(loaded.intValue == 42, "intValue should be 42");
            Debug.Assert(Mathf.Approximately(loaded.floatValue, 3.14f), "floatValue should be 3.14");
            Debug.Assert(loaded.strValue == "Hello", "strValue should be 'Hello'");

            // 3. �޸Ĳ�����
            loaded.intValue = 100;
            PersistentDataManager.Instance.SaveData(testKey, loaded);

            // 4. �ٴμ��ز�����
            var loaded2 = PersistentDataManager.Instance.LoadData<TestData>(testKey);
            Debug.Assert(loaded2.intValue == 100, "intValue should be updated to 100");

            // 5. ����������DeleteData�����ɵ��ã�
            // PersistentDataManager.Instance.DeleteData(testKey);
        }

        static void KActionTest()
        {
            KSignal mySignal = new();
            KSignal<int> mySignalWithParam = new();
            KSignal<int, string> mySignalWithTwoParams = new();

            var subscriber = new Subscriber();
            Guid handle;
            var TestResult = 0;
            var TestResult2 = 0;
            var TestResult3 = 0;

            // Subscriber to an Action
            mySignal.Connect(subscriber, () =>
            {
                TestResult = 1;
                Debug.Log("1");
            });
            handle = mySignal.Connect(subscriber, () =>
            {
                TestResult2 = 1;
                Debug.Log("2");
            });
            mySignal.Connect(subscriber, () =>
            {
                Debug.Log("3");
                TestResult3 = 1;
            });


            // Trigger the Actions
            mySignal?.Invoke();
            Debug.Assert(TestResult == 1);
            Debug.Assert(TestResult2 == 1);
            Debug.Assert(TestResult3 == 1);
            mySignal.Disconnect(handle);

            TestResult2 = 0;
            // Test unscribe
            mySignal?.Invoke();
            Debug.Assert(TestResult2 == 0);
            TestResult = 0;
            TestResult2 = 0;
            TestResult3 = 0;
            subscriber.DisconnectAll();
            mySignal?.Invoke();
            Debug.Assert(TestResult == 0);
            Debug.Assert(TestResult2 == 0);
            Debug.Assert(TestResult3 == 0);

            subscriber.DisconnectAll();
        }
    }
}