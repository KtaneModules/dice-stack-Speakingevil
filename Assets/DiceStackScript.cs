using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class DiceStackScript : MonoBehaviour {

    public KMAudio Audio;
    public KMBombModule module;
    public Transform centre;
    public List<KMSelectable> buttons;
    public GameObject[] dfaces;
    public Transform[] dice;
    public Renderer[] drends;
    public TextMesh[] displays;
    public GameObject selectableHolder;

    private readonly Vector2[] fpos = new Vector2[6] { new Vector2(-90, 0), new Vector2(0, 180), new Vector2(0, 90), new Vector2(0, 0), new Vector2(0, -90), new Vector2(90, 0)};
    private int[][] faces = new int[4][];
    private int[] sums = new int[4];
    private int dselect;
    private bool pressable = true;

    private static int moduleIDCounter;
    private int moduleID;

    private Color Colour(Color c, int shift)
    {
        float[] rgb = new float[3] { c.r, c.g, c.b };
        if (rgb.All(x => x > 0.9f))
            return new Color(0.25f, 0.25f, 0.25f);
        if (shift > 3)
            rgb[0] += 0.25f;
        if ((shift / 2) % 2 > 0)
            rgb[1] += 0.25f;
        if (shift % 2 > 0)
            rgb[2] += 0.25f;
        return new Color(rgb[0], rgb[1], rgb[2]);
    }

    private int[] RotateDice(int[] d, int dir)
    {
        switch (dir)
        {
            case 0: return new int[6] { d[3], d[0], d[2], d[5], d[4], d[1]};
            case 1: return new int[6] { d[4], d[1], d[0], d[3], d[5], d[2]};
            case 2: return new int[6] { d[1], d[5], d[2], d[0], d[4], d[3]};
            default: return new int[6] {d[2], d[1], d[5], d[3], d[0], d[4]};
        }
    }

    private void Awake()
    {
        moduleID = ++moduleIDCounter;
        Color[] cols = new Color[4];
        cols[0] = new Color(0.25f, 0.25f, 0.25f);
        int[][] rots = new int[4][];
        string[] ans = new string[4];
        for (int i = 0; i < 4; i++)
        {
            faces[i] = Enumerable.Range(1, 9).ToArray().Shuffle().Take(6).ToArray();
            cols[i] = Colour(cols[i > 1 ? i - 1 : 0], Random.Range(1, 8));
            rots[i] = new int[2] { Random.Range(0, 4), Random.Range(0, 4)};
            ans[i] = string.Format("\n[Dice Stack #{0}] {1} on top, {2} facing down.", moduleID, faces[i][0], faces[i][1]);
        }
        cols.Shuffle();
        for (int i = 0; i < 4; i++)
        {
            int s = faces.Select(x => x[i + 1]).Sum();
            sums[i] = s;
            displays[i].text = s.ToString();
            for (int j = 0; j < 10; j++)
            {
                if (j == 0 || faces[i].Contains(j))
                    drends[(10 * i) + j].material.color = cols[i];
                else
                    dfaces[(9 * i) + j - 1].SetActive(false);
            }
        }
        for(int i = 0; i < 4; i++)
        { 
            for (int j = 0; j < 2; j++)
                for (int k = 0; k < rots[i][j]; k++)
                    faces[i] = RotateDice(faces[i], j);
            for (int j = 0; j < 6; j++)
                dfaces[(9 * i) + faces[i][j] - 1].transform.localEulerAngles = new Vector3(fpos[j].x, fpos[j].y, 90 * Random.Range(0, 4));
        }
        Debug.LogFormat("[Dice Stack #{0}] The numbers on the faces of the four dice are:\n[Dice Stack #{0}] {1}", moduleID, string.Join("\n[Dice Stack #"+ moduleID + "] ", faces.Select(x => string.Join(", ", x.Select(y => y.ToString()).ToArray())).ToArray()));
        Debug.LogFormat("[Dice Stack #{0}] The required sums of the face numbers are: {1}", moduleID, string.Join(", ", sums.Select(x => x.ToString()).ToArray()));
        Debug.LogFormat("[Dice Stack #{0}] Set the dice to the following positions:{1}", moduleID, string.Join("", ans));
        foreach(KMSelectable button in buttons)
        {
            int b = buttons.IndexOf(button);
            button.OnInteract = delegate ()
            {
                if (pressable)
                {
                    if (b > 3)
                        StartCoroutine("Submit");
                    else
                    {
                        faces[dselect] = RotateDice(faces[dselect], b);
                        StartCoroutine(DiceRotate(dselect, b));
                    }
                }
                return false;
            };
        }
        buttons[4].OnInteractEnded = delegate ()
        {
            if (pressable)
            {
                StopCoroutine("Submit");
                StartCoroutine(DicePush());
            }
        };
    }

    private IEnumerator DiceRotate(int x, int d)
    {
        pressable = false;
        selectableHolder.SetActive(false);
        Audio.PlaySoundAtTransform("Rotate", dice[x]);       
        Vector3 axis = new Vector3[4] { -centre.right, -centre.forward, centre.right, centre.forward }[d];
        float e = 0;
        while(e < 90)
        {
            float dt = Time.deltaTime * 360;
            e += dt;
            dice[x].RotateAround(centre.position, axis, dt);
            yield return null;
        }
        dice[x].RotateAround(centre.position, axis, -e);
        for (int j = 0; j < 6; j++)
        {
            float dz = dfaces[(9 * dselect) + faces[dselect][j] - 1].transform.localEulerAngles.z;
            dfaces[(9 * dselect) + faces[dselect][j] - 1].transform.localEulerAngles = new Vector3(fpos[j].x, fpos[j].y, d % 2 == 0 ? dz : 90 - dz);
        }
        selectableHolder.SetActive(true);
        pressable = true;
    }

    private IEnumerator DicePush()
    {
        pressable = false;
        Audio.PlaySoundAtTransform("StackDown", transform);
        selectableHolder.SetActive(false);
        dselect += 3;
        dselect %= 4;
        Vector3 irot = dice[dselect].localEulerAngles;
        Vector3 r = new Vector3(Random.Range(-270, 270), Random.Range(-270, 270), Random.Range(-270, 270));
        float e = 0;
        while(e < 0.5f)
        {
            float d = Time.deltaTime;
            e += d;
            for(int i = 0; i < 4; i++)
            {
                if (dselect == i)
                {
                    dice[i].localEulerAngles += r * d;
                    float s = (1 - (2 * e)) * 0.02f;
                    dice[i].localScale = new Vector3(s, s, s);
                }
                else
                    dice[i].localPosition -= new Vector3(0, 0.04f * d, 0);
            }
            yield return null;
        }
        dice[dselect].localPosition = new Vector3(0, 0.159f, 0);
        Vector3 p = dice[dselect].localEulerAngles;
        while (e < 1)
        {
            float d = Time.deltaTime;
            e += d;
            for (int i = 0; i < 4; i++)
            {
                if (dselect == i)
                {
                    dice[i].localEulerAngles = 2 * p * (1 - e);
                    float s = 0.04f * (e - 0.5f);
                    dice[i].localScale = new Vector3(s, s, s);
                }
                else
                    dice[i].localPosition -= new Vector3(0, 0.04f * d, 0);
            }
            yield return null;
        }
        for(int i = 0; i < 4; i++)
        {
            int x = (dselect + i) % 4;
            dice[x].localPosition = new Vector3(0, 0.159f - (0.04f * i), 0);
        }
        dice[dselect].localEulerAngles = irot;
        selectableHolder.SetActive(true);
        pressable = true;
    }

    private IEnumerator Submit()
    {
        yield return new WaitForSeconds(1);
        pressable = false;
        selectableHolder.SetActive(false);
        bool[] correct = new bool[4];
        for(int i = 0; i < 4; i++)
        {
            int s = faces.Select(x => x[i + 1]).Sum();
            correct[i] = sums[i] == s;
            displays[i].color = correct[i] ? new Color(0, 1, 0) : new Color(1, 0, 0);
        }
        if(correct.All(x => x))
        {
            module.HandlePass();
            Audio.PlaySoundAtTransform("Solve", transform);
            for (int i = 0; i < 4; i++)
                displays[i].text = "\u2714";
            for(int i = 0; i < 4; i++)
            {
                int x = (dselect + i) % 4;
                Vector3 r = new Vector3(Random.Range(-270, 270), Random.Range(-270, 270), Random.Range(-270, 270));
                float e = 1;
                while(e > 0)
                {
                    e -= Time.deltaTime;
                    dice[x].localScale = new Vector3(e, e, e) * 0.02f;
                    dice[x].localEulerAngles += r * Time.deltaTime;
                    yield return null;
                }
                for (int j = 0; j < 10; j++)
                    drends[(10 * ((dselect + i) % 4)) + j].enabled = false;
            }
        }
        else
        {
            module.HandleStrike();
            yield return new WaitForSeconds(2);
            for (int i = 0; i < 4; i++)
                displays[i].color = new Color(1, 1, 1);
            selectableHolder.SetActive(true);
            pressable = true;
        }
    }
#pragma warning disable 414
    private readonly string TwitchHelpMessage = @"!{0} <Up/Right/Down/Left/Push> [Rotates die with URDL. Moves stack down one die with Push. Chain with spaces.] | !{0} submit";
#pragma warning restore 414

    private IEnumerator ProcessTwitchCommand(string command)
    {
        if(command.ToLowerInvariant() == "submit")
        {
            yield return null;
            buttons[4].OnInteract();
            yield return new WaitForSeconds(1);
        }
        string[] commands = command.ToUpperInvariant().Split(' ').Select(x => x[0].ToString()).ToArray();
        List<int> s = new List<int> { };
        for(int i = 0; i < commands.Length; i++)
        {
            if (commands[i].Length < 1)
                continue;
            int d = "DRULP".IndexOf(commands[i]);
            if(d < 0)
            {
                yield return "sendtochaterror!f Command " + i + " is invalid.";
                yield break;
            }
            if (s.Count() > 0 && d < 4 && s.Last() < 4 && Mathf.Abs(d - s.Last()) == 2)
                s.RemoveAt(s.Count() - 1);
            else
                s.Add(d);
            if (s.Count() > 3 && s.TakeLast(4).Distinct().Count() < 2)
                s = s.Take(s.Count() - 4).ToList();
        }
        if (s.Count() < 1)
            yield return "sendtochaterror!f All commands cancel out.";
        for(int i = 0; i < s.Count(); i++)
        {
            while (!pressable)
                yield return true;
            yield return null;
            buttons[s[i]].OnInteract();
            yield return null;
            if (s[i] > 3)
                buttons[4].OnInteractEnded();
        }
    }
}
