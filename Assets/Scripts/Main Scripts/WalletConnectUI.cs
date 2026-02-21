using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WalletConnectUI : MonoBehaviour
{
    const string RootObjectName = "WalletConnectUIRoot";
    const string ButtonObjectName = "WalletConnectButton";
    const string StatusObjectName = "WalletConnectStatus";
    const string SolanaWeb3TypeName = "Solana.Unity.SDK.Web3";

    Button connectButton;
    TMP_Text buttonLabel;
    TMP_Text statusLabel;
    bool isConnecting;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Bootstrap()
    {
        if (FindObjectOfType<WalletConnectUI>() != null) return;

        GameObject root = new GameObject(nameof(WalletConnectUI));
        root.AddComponent<WalletConnectUI>();
    }

    void Start()
    {
        BuildOrFindWidget();
        RefreshConnectionStatus();
    }

    void BuildOrFindWidget()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("WalletConnectUI: Canvas not found. Wallet button was not created.");
            return;
        }

        Transform existingRoot = canvas.transform.Find(RootObjectName);
        GameObject rootObject = existingRoot != null ? existingRoot.gameObject : CreateRoot(canvas.transform);

        Transform buttonTransform = rootObject.transform.Find(ButtonObjectName);
        if (buttonTransform == null)
            buttonTransform = CreateButton(rootObject.transform).transform;

        connectButton = buttonTransform.GetComponent<Button>();
        buttonLabel = buttonTransform.GetComponentInChildren<TMP_Text>(true);
        connectButton.onClick.RemoveListener(OnConnectPressed);
        connectButton.onClick.AddListener(OnConnectPressed);

        Transform statusTransform = rootObject.transform.Find(StatusObjectName);
        if (statusTransform == null)
            statusTransform = CreateStatus(rootObject.transform).transform;

        statusLabel = statusTransform.GetComponent<TMP_Text>();
    }

    GameObject CreateRoot(Transform parent)
    {
        GameObject root = new GameObject(RootObjectName, typeof(RectTransform), typeof(Image));
        root.transform.SetParent(parent, false);

        RectTransform rect = root.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.sizeDelta = new Vector2(280f, 96f);
        rect.anchoredPosition = new Vector2(-16f, -76f);

        Image background = root.GetComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.28f);
        return root;
    }

    GameObject CreateButton(Transform parent)
    {
        GameObject buttonObject = new GameObject(ButtonObjectName, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, 44f);
        rect.anchoredPosition = new Vector2(0f, 0f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.9f, 0.55f, 0.05f, 0.95f);

        GameObject textObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(buttonObject.transform, false);

        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.text = "Connect Wallet";
        text.alignment = TextAlignmentOptions.Center;
        text.fontSize = 20f;
        text.color = Color.white;

        return buttonObject;
    }

    GameObject CreateStatus(Transform parent)
    {
        GameObject statusObject = new GameObject(StatusObjectName, typeof(RectTransform), typeof(TextMeshProUGUI));
        statusObject.transform.SetParent(parent, false);

        RectTransform rect = statusObject.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.sizeDelta = new Vector2(-12f, 42f);
        rect.anchoredPosition = new Vector2(0f, 8f);

        TextMeshProUGUI status = statusObject.GetComponent<TextMeshProUGUI>();
        status.text = "Wallet: not connected";
        status.alignment = TextAlignmentOptions.Left;
        status.fontSize = 17f;
        status.color = Color.white;
        status.enableWordWrapping = false;

        return statusObject;
    }

    async void OnConnectPressed()
    {
        if (isConnecting) return;
        isConnecting = true;

        SetButtonEnabled(false);
        SetStatus("Wallet: connecting...");

        try
        {
            bool connected = await TryConnectWallet();
            if (!connected) return;

            string address = TryReadConnectedAddress();
            bool hasAddress = !string.IsNullOrWhiteSpace(address);
            SetStatus(hasAddress ? $"Wallet: {ShortAddress(address)}" : "Wallet: connected");
            SetButtonText(hasAddress ? "Connected" : "Reconnect Wallet");
        }
        catch (Exception exception)
        {
            Debug.LogError($"WalletConnectUI: Wallet connect failed: {exception.Message}");
            SetStatus("Wallet: connect failed");
        }
        finally
        {
            isConnecting = false;
            SetButtonEnabled(true);
        }
    }

    async Task<bool> TryConnectWallet()
    {
        Type web3Type = ResolveWeb3Type();
        if (web3Type == null)
        {
            SetStatus("Wallet: Solana SDK missing");
            Debug.LogWarning("WalletConnectUI: Solana.Unity.SDK not found. Install com.solana.unity-sdk.");
            return false;
        }

        object web3Instance = GetWeb3Instance(web3Type);
        if (web3Instance == null)
        {
            SetStatus("Wallet: Web3 init failed");
            return false;
        }

        MethodInfo loginMethod = FindMethod(web3Type, new[]
        {
            "LoginWalletAdapterWithSIWS",
            "LoginWalletAdapter",
            "LoginWallet",
            "ConnectWallet"
        });

        if (loginMethod == null)
        {
            SetStatus("Wallet: login API not found");
            Debug.LogWarning("WalletConnectUI: No wallet login method found on Solana Web3 object.");
            return false;
        }

        object invocationResult = loginMethod.Invoke(web3Instance, null);
        await AwaitUnknownAsyncResult(invocationResult);
        return true;
    }

    static Task AwaitUnknownAsyncResult(object asyncResult)
    {
        if (asyncResult == null) return Task.CompletedTask;
        if (asyncResult is Task task) return task;

        MethodInfo asTaskMethod = asyncResult.GetType().GetMethod("AsTask", Type.EmptyTypes);
        if (asTaskMethod == null) return Task.CompletedTask;

        object taskObject = asTaskMethod.Invoke(asyncResult, null);
        return taskObject as Task ?? Task.CompletedTask;
    }

    void RefreshConnectionStatus()
    {
        string address = TryReadConnectedAddress();
        bool connected = !string.IsNullOrWhiteSpace(address);
        SetStatus(connected ? $"Wallet: {ShortAddress(address)}" : "Wallet: not connected");
        SetButtonText(connected ? "Connected" : "Connect Wallet");
    }

    string TryReadConnectedAddress()
    {
        Type web3Type = ResolveWeb3Type();
        if (web3Type == null) return null;

        object web3Instance = GetWeb3Instance(web3Type);
        if (web3Instance == null) return null;

        object[] accountCandidates =
        {
            GetMemberValue(web3Instance, "Account"),
            GetMemberValue(GetMemberValue(web3Instance, "Wallet"), "Account"),
            GetMemberValue(GetMemberValue(web3Instance, "BaseWallet"), "Account"),
            GetMemberValue(web3Instance, "PublicKey")
        };

        foreach (object candidate in accountCandidates)
        {
            string address = ExtractAddress(candidate);
            if (!string.IsNullOrWhiteSpace(address)) return address;
        }

        return null;
    }

    static string ExtractAddress(object candidate)
    {
        if (candidate == null) return null;
        if (candidate is string direct && !string.IsNullOrWhiteSpace(direct)) return direct;

        object[] addressCandidates =
        {
            GetMemberValue(candidate, "PublicKey"),
            GetMemberValue(candidate, "PublicKeyString"),
            GetMemberValue(candidate, "Address"),
            GetMemberValue(candidate, "Key")
        };

        foreach (object value in addressCandidates)
        {
            if (value == null) continue;

            string text = value as string ?? value.ToString();
            if (!string.IsNullOrWhiteSpace(text)) return text;
        }

        string fallback = candidate.ToString();
        return string.IsNullOrWhiteSpace(fallback) ? null : fallback;
    }

    static Type ResolveWeb3Type()
    {
        Type direct = Type.GetType($"{SolanaWeb3TypeName}, Solana.Unity.SDK");
        if (direct != null) return direct;

        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            Type found = assembly.GetType(SolanaWeb3TypeName);
            if (found != null) return found;
        }

        return null;
    }

    static object GetWeb3Instance(Type web3Type)
    {
        object instance = GetMemberValue(web3Type, null, "Instance");
        if (instance != null) return instance;

        if (!typeof(Component).IsAssignableFrom(web3Type)) return null;

        GameObject host = GameObject.Find("SolanaWeb3Host");
        if (host == null)
        {
            host = new GameObject("SolanaWeb3Host");
            DontDestroyOnLoad(host);
        }

        Component component = host.GetComponent(web3Type) ?? host.AddComponent(web3Type);
        return GetMemberValue(web3Type, null, "Instance") ?? component;
    }

    static MethodInfo FindMethod(Type type, IReadOnlyList<string> names)
    {
        foreach (string name in names)
        {
            MethodInfo method = type.GetMethod(name, BindingFlags.Public | BindingFlags.Instance);
            if (method != null && method.GetParameters().Length == 0)
                return method;
        }

        return null;
    }

    static object GetMemberValue(object target, string memberName)
    {
        if (target == null || string.IsNullOrWhiteSpace(memberName)) return null;

        Type targetType = target as Type ?? target.GetType();
        object instance = target is Type ? null : target;
        return GetMemberValue(targetType, instance, memberName);
    }

    static object GetMemberValue(Type targetType, object instance, string memberName)
    {
        PropertyInfo property = targetType.GetProperty(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        if (property != null) return property.GetValue(instance);

        FieldInfo field = targetType.GetField(memberName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);
        if (field != null) return field.GetValue(instance);

        return null;
    }

    static string ShortAddress(string address)
    {
        if (string.IsNullOrWhiteSpace(address) || address.Length <= 12) return address;
        return $"{address.Substring(0, 6)}...{address.Substring(address.Length - 4)}";
    }

    void SetButtonText(string text)
    {
        if (buttonLabel != null) buttonLabel.text = text;
    }

    void SetStatus(string text)
    {
        if (statusLabel != null) statusLabel.text = text;
    }

    void SetButtonEnabled(bool enabled)
    {
        if (connectButton != null) connectButton.interactable = enabled;
    }
}
