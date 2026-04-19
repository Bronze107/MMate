using UnityEngine;
using MMate.Core.LLM;
using MMate.Core.State;
using MMate.Interaction;
using MMate.Avatar;
using System.Collections.Generic;

public class Phase1QuickTest : MonoBehaviour
{
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.F5))
            TestLLMNonStreaming();
        if (Input.GetKeyDown(KeyCode.F9))
            TestLLMStreaming();
        if (Input.GetKeyDown(KeyCode.Alpha3))
            TestStateMachine();
        if (Input.GetKeyDown(KeyCode.Alpha4))
            TestChatManager();
        if (Input.GetKeyDown(KeyCode.Alpha5))
            TestAvatarAnimation();
    }

    async void TestLLMNonStreaming()
    {
        var client = FindObjectOfType<OpenAICompatibleClient>();
        Debug.Log($"[Test1] TestLLMNonStreaming");
        var result = await client.SendAsync(new List<ChatMessage>
        {
            new ChatMessage("user", "Say hello")
        });
        Debug.Log($"[Test1] Result: {result}");
    }

    async void TestLLMStreaming()
    {
        var client = FindObjectOfType<OpenAICompatibleClient>();
        Debug.Log($"[Test1] TestLLMStreaming");
        await client.SendStreamingAsync(new List<ChatMessage>
        {
            new ChatMessage("user", "Count 1 to 5")
        }, chunk => Debug.Log($"[Test2] Chunk: {chunk}"));
    }

    void TestStateMachine()
    {
        var sm = AvatarStateMachine.Instance;
        sm.ChangeState(new OnlineState());
        sm.ChangeState(new IdleState());
        sm.ChangeState(new OfflineState());
    }

    void TestChatManager()
    {
        ChatManager.Instance.SendMessage("Test message from script");
    }

    void TestAvatarAnimation()
    {
        var avatar = FindObjectOfType<AvatarController>();
        avatar?.PlayIdle();
        avatar?.PlayTalk();
    }
}