using System;
using System.Linq;
using GoogleOAuth.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(menuName = "GoogleOAuth/Demo")]
public class Example : ScriptableObject
{
    [SerializeField] private string _clientId;
    [SerializeField] private string _clientSecret;

    private AuthorizationCodeProvider.Result _authorizationResult;
    private AccessTokenProvider.Result _accessTokenResult;
    
    public async void AuthorizeAsync()
    {
        Assert.That(_clientId, Is.Not.Null.Or.Empty);
        
        var provider = new AuthorizationCodeProvider(_clientId);
        var handle = await provider.ProvideAsync();
        if (!handle.IsFailed)
        {
            _authorizationResult = handle.Result;
        }
        provider.Dispose();
        
        Debug.Log($"Authorization Code: {_authorizationResult.AuthorizationCode}");
    }

    public async void GetAccessTokenAsync()
    {
        Assert.That(_clientId, Is.Not.Null.Or.Empty);
        Assert.That(_clientSecret, Is.Not.Null.Or.Empty);
        Assert.That(_authorizationResult, Is.Not.Null);
        
        var provider = new AccessTokenProvider(_clientId, _clientSecret);
        var handle = await provider.ProvideAsync(_authorizationResult.AuthorizationCode, _authorizationResult.CodeVerifier, _authorizationResult.RedirectUri);
        if (!handle.IsFailed)
        {
            _accessTokenResult = handle.Result;
        }
        
        Debug.Log($"Access Token: {_accessTokenResult.AccessToken}");
    }

    public async void RefreshAccessTokenAsync()
    {
        Assert.That(_clientId, Is.Not.Null.Or.Empty);
        Assert.That(_clientSecret, Is.Not.Null.Or.Empty);
        Assert.That(_accessTokenResult, Is.Not.Null);
        
        var provider = new RefreshedAccessTokenProvider(_clientId, _clientSecret);
        var handle = await provider.ProvideAsync(_accessTokenResult.RefreshToken);
        
        Debug.Log($"Access Token: {handle.Result.AccessToken}");
    }
}

[CustomEditor(typeof(Example))]
public class DemoInspector : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        var component = (Example) target;
        var clientIdProp = serializedObject.FindProperty("_clientId");
        var clientSecretProp = serializedObject.FindProperty("_clientSecret");

        using (new GUILayout.VerticalScope(GUI.skin.box))
        {
            EditorGUILayout.LabelField("Initialize", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(clientIdProp);
            EditorGUILayout.PropertyField(clientSecretProp);
            
            if (GUILayout.Button("Authorize"))
            {
                component.AuthorizeAsync();
            }
            if (GUILayout.Button("Get Access Token"))
            {
                component.GetAccessTokenAsync();
            }
            if (GUILayout.Button("Refresh Access Token"))
            {
                component.RefreshAccessTokenAsync();
            }
        }

        serializedObject.ApplyModifiedProperties();
    }
}