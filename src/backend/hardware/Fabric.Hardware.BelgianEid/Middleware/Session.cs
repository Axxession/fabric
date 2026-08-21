#nullable disable
using Fabric.Hardware.BelgianEid.Middleware.Objects;
using Fabric.Hardware.BelgianEid.Middleware.Wrapper;

namespace Fabric.Hardware.BelgianEid.Middleware;

/// <summary>
///     Represents an open Session with a Token.
/// </summary>
public class Session : IDisposable
{
    #region Members

    #endregion

    #region Properties

    /// <summary>
    ///     Session's Token
    /// </summary>
    public Token Token { get; }

    /// <summary>
    ///     Session's Cryptoki Module
    /// </summary>
    public Module Module => Token.Module;

    /// <summary>
    ///     Session Handle / id
    /// </summary>
    public uint HSession { get; }

    #endregion

    #region Methods

    #region Instance

    /// <summary>
    ///     Constructor
    /// </summary>
    /// <param name="token">Session's Token</param>
    /// <param name="hSession">Session Handle / Id</param>
    public Session(Token token, uint hSession)
    {
        Token = token;
        HSession = hSession;
    }

    #endregion

    #region Authentication

    public void Login(UserType userType, string pwd)
    {
        Module.P11Module.Login(HSession, (CKU)userType, pwd);
    }

    public void Logout()
    {
        Module.P11Module.Logout(HSession);
    }

    #endregion

    #region Initialization

    public void SetPIN(string oldPIN, string newPIN)
    {
        Module.P11Module.SetPIN(HSession, oldPIN, newPIN);
    }

    public void InitPIN(string pin)
    {
        Module.P11Module.InitPIN(HSession, pin);
    }

    #endregion

    #region Encipher

    #region Digest

    public void DigestInit(Mechanism mechanism)
    {
        Module.P11Module.DigestInit(HSession, mechanism.CK_MECHANISM);
    }

    public void DigestUpdate(byte[] data)
    {
        Module.P11Module.DigestUpdate(HSession, data);
    }

    public byte[] Digest(byte[] data)
    {
        return Module.P11Module.Digest(HSession, data);
    }

    public byte[] DigestFinal()
    {
        return Module.P11Module.DigestFinal(HSession);
    }

    #endregion

    #region Encrypt

    public void EncryptInit(Mechanism mechanism, PublicKey key)
    {
        Module.P11Module.EncryptInit(HSession, mechanism.CK_MECHANISM, key.HObj);
    }

    public void EncryptInit(Mechanism mechanism, SecretKey key)
    {
        Module.P11Module.EncryptInit(HSession, mechanism.CK_MECHANISM, key.HObj);
    }

    public byte[] Encrypt(byte[] data)
    {
        return Module.P11Module.Encrypt(HSession, data);
    }

    public byte[] EncryptUpdate(byte[] data)
    {
        return Module.P11Module.EncryptUpdate(HSession, data);
    }

    public byte[] EncryptFinal()
    {
        return Module.P11Module.EncryptFinal(HSession);
    }

    #endregion

    #region Decrypt

    public void DecryptInit(Mechanism mechanism, PrivateKey key)
    {
        Module.P11Module.DecryptInit(HSession, mechanism.CK_MECHANISM, key.HObj);
    }

    public void DecryptInit(Mechanism mechanism, SecretKey key)
    {
        Module.P11Module.DecryptInit(HSession, mechanism.CK_MECHANISM, key.HObj);
    }

    public byte[] Decrypt(byte[] data)
    {
        return Module.P11Module.Decrypt(HSession, data);
    }

    public byte[] DecryptUpdate(byte[] data)
    {
        return Module.P11Module.DecryptUpdate(HSession, data);
    }

    public byte[] DecryptFinal()
    {
        return Module.P11Module.DecryptFinal(HSession);
    }

    #endregion

    #region Signature

    public void SignInit(Mechanism signingMechanism, PrivateKey key)
    {
        Module.P11Module.SignInit(HSession, signingMechanism.CK_MECHANISM, key.HObj);
    }

    public void SignUpdate(byte[] data)
    {
        Module.P11Module.SignUpdate(HSession, data);
    }

    public byte[] SignFinal()
    {
        return Module.P11Module.SignFinal(HSession);
    }

    public byte[] Sign(byte[] data)
    {
        return Module.P11Module.Sign(HSession, data);
    }

    #endregion

    #region Verification

    public void VerifyInit(Mechanism signingMechanism, PublicKey key)
    {
        Module.P11Module.VerifyInit(HSession, signingMechanism.CK_MECHANISM, key.HObj);
    }

    public void VerifyInit(Mechanism signingMechanism, Certificate certificate)
    {
        Module.P11Module.VerifyInit(HSession, signingMechanism.CK_MECHANISM, certificate.HObj);
    }

    public void VerifyUpdate(byte[] data)
    {
        Module.P11Module.VerifyUpdate(HSession, data);
    }

    public bool VerifyFinal(byte[] signature)
    {
        try
        {
            Module.P11Module.VerifyFinal(HSession, signature);
            return true;
        }
        catch (TokenException tex)
        {
            if (tex.ErrorCode == CKR.SIGNATURE_INVALID)
                return false;
            throw;
        }
    }

    public bool Verify(byte[] data, byte[] signature)
    {
        try
        {
            Module.P11Module.Verify(HSession, data, signature);
            return true;
        }
        catch (TokenException tex)
        {
            if (tex.ErrorCode == CKR.SIGNATURE_INVALID)
                return false;
            throw;
        }
    }

    #endregion

    #region Key Generation

    public SecretKey GenerateKey(Mechanism mech, P11Object template)
    {
        var hKey = Module.P11Module.GenerateKey(HSession, mech.CK_MECHANISM, getAssignedAttributes(template));
        return (SecretKey)SecretKey.GetInstance(this, hKey);
    }

    public KeyPair GenerateKeyPair(Mechanism mech, P11Object pubTemplate, P11Object privTemplate)
    {
        var hkp = Module.P11Module.GenerateKeyPair(
            HSession,
            mech.CK_MECHANISM,
            getAssignedAttributes(pubTemplate),
            getAssignedAttributes(privTemplate)
        );

        return new KeyPair((PublicKey)PublicKey.GetInstance(this, hkp.hPublicKey), (PrivateKey)PrivateKey.GetInstance(this, hkp.hPrivateKey));
    }

    #endregion

    #endregion

    #region Objects

    #region Search

    public void FindObjectsInit(params P11Attribute[] attrs)
    {
        var ckAttrs = P11Util.ConvertToCK_ATTRIBUTEs(attrs);
        Module.P11Module.FindObjectsInit(HSession, ckAttrs);
    }

    public P11Object[] FindObjects(uint maxCount)
    {
        var hObjs = Module.P11Module.FindObjects(HSession, maxCount);
        var objs = new P11Object[hObjs.Length];
        for (var i = 0; i < hObjs.Length; ++i)
            objs[i] = P11Object.GetInstance(this, hObjs[i]);

        return objs;
    }

    public void FindObjectsFinal()
    {
        Module.P11Module.FindObjectsFinal(HSession);
    }

    #endregion

    #region Management

    public P11Object CreateObject(P11Object template)
    {
        var hObj = Module.P11Module.CreateObject(HSession, getAssignedAttributes(template));
        return P11Object.GetInstance(this, hObj);
    }

    public void DestroyObject(P11Object obj)
    {
        Module.P11Module.DestroyObject(HSession, obj.HObj);
    }

    #endregion

    #endregion

    #region General

    public void Dispose()
    {
        CloseSession();
    }

    private void CloseSession()
    {
        Module.P11Module.CloseSession(HSession);
    }

    private static CK_ATTRIBUTE[] getAssignedAttributes(P11Object obj)
    {
        var props = obj.GetType().GetProperties();
        var attrs = new List<CK_ATTRIBUTE>();
        for (var i = 0; i < props.Length; i++)
        {
            var val = props[i].GetValue(obj, null) as P11Attribute;
            if (val != null && val.IsAssigned)
                attrs.Add(val.CK_ATTRIBUTE);
        }

        return attrs.ToArray();
    }

    #endregion

    #endregion
}
