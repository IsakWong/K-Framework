using System.Collections;
using Framework.Coroutine;
using UnityEngine;

/// <summary>
/// CoroutineHandler 简单测试示例
/// </summary>
public class CoroutineHandlerTest : MonoBehaviour
{
    private CoroutineHandler _handler = new CoroutineHandler();
    private KCoroutine _testCoroutine;
    
    private void Start()
    {
        Debug.Log("=== CoroutineHandler 测试开始 ===");
        
        // 测试 1: 启动简单协程
        _testCoroutine = _handler.StartCoroutine(SimpleCoroutine());
        
        // 测试 2: 启动多个协程
        _handler.StartCoroutine(CountCoroutine("A", 3));
        _handler.StartCoroutine(CountCoroutine("B", 5));
        
        Debug.Log($"活跃协程数量: {_handler.ActiveCoroutineCount}");
    }
    
    private void Update()
    {
        // 手动驱动协程执行
        _handler.Tick(Time.deltaTime);
        
        // 测试暂停/恢复
        if (Input.GetKeyDown(KeyCode.P))
        {
            _testCoroutine?.Pause();
            Debug.Log("协程已暂停");
        }
        
        if (Input.GetKeyDown(KeyCode.R))
        {
            _testCoroutine?.Resume();
            Debug.Log("协程已恢复");
        }
        
        if (Input.GetKeyDown(KeyCode.S))
        {
            _handler.StopCoroutine(_testCoroutine);
            Debug.Log("协程已停止");
        }
        
        if (Input.GetKeyDown(KeyCode.C))
        {
            _handler.Clear();
            Debug.Log("所有协程已清理");
        }
    }
    
    private IEnumerator SimpleCoroutine()
    {
        Debug.Log("[Simple] 开始");
        
        yield return new WaitSeconds(1f);
        Debug.Log("[Simple] 1秒后");
        
        yield return new WaitSeconds(2f);
        Debug.Log("[Simple] 又过了2秒");
        
        Debug.Log("[Simple] 结束");
    }
    
    private IEnumerator CountCoroutine(string name, int count)
    {
        for (int i = 0; i < count; i++)
        {
            Debug.Log($"[{name}] Count: {i}");
            yield return new WaitForSeconds(0.5f);
        }
        Debug.Log($"[{name}] 完成!");
    }
    
    private void OnDestroy()
    {
        _handler.Clear();
    }
}

