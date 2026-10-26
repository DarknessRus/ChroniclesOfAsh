using UnityEngine;
using UnityEngine.UI;

/// <summary>Creates four distinct painterly fantasy portraits at runtime, requiring no external asset downloads.</summary>
public static class AshHeroPortraits
{
    static readonly Sprite[] cached = new Sprite[4];
    static readonly Color[] accents = {
        new Color(.98f,.30f,.08f), new Color(.12f,.58f,1f),
        new Color(.30f,.78f,.39f), new Color(.58f,.28f,.86f)
    };

    public static Sprite Create(int kind)
    {
        kind = Mathf.Clamp(kind,0,3);
        if(cached[kind] != null) return cached[kind];
        const int w=256, h=320;
        var tex=new Texture2D(w,h,TextureFormat.RGBA32,false);
        tex.name="Chronicles of Ash Hero Portrait "+kind;
        tex.filterMode=FilterMode.Bilinear;
        tex.wrapMode=TextureWrapMode.Clamp;
        Color accent=accents[Mathf.Clamp(kind,0,3)];
        var pixels=new Color[w*h];
        for(int y=0;y<h;y++) for(int x=0;x<w;x++)
        {
            float nx=x/(float)w, ny=y/(float)h;
            float glow=Mathf.Max(0,1f-Mathf.Sqrt((nx-.5f)*(nx-.5f)*3f+(ny-.62f)*(ny-.62f)*1.8f));
            float noise=Mathf.PerlinNoise(x*.045f+kind*13,y*.05f+kind*7)*.07f;
            pixels[y*w+x]=new Color(.018f+glow*(accent.r*.18f)+noise*.2f,
                .026f+glow*(accent.g*.17f)+noise*.22f,.035f+glow*(accent.b*.17f)+noise*.24f,1);
        }
        tex.SetPixels(pixels);
        // Distant ruined towers and fiery horizon.
        for(int i=0;i<11;i++)
        {
            int x=8+i*24; int towerH=24+(i*37%68);
            Rect(tex,x,0,13,towerH,new Color(.055f,.065f,.07f));
            Tri(tex,x-5,towerH,x+18,towerH,x+6,towerH+23,new Color(.07f,.075f,.08f));
            if(i%3==0) Rect(tex,x+4,towerH/2,3,10,accent*.7f);
        }
        // Cloak silhouette, layered shoulders and torso.
        Ellipse(tex,128,110,103,117,new Color(.035f,.045f,.055f));
        Tri(tex,26,0,230,0,128,210,kind==2?new Color(.055f,.12f,.075f):new Color(.075f,.055f,.065f));
        Tri(tex,38,66,218,66,128,222,new Color(.11f,.12f,.13f));
        // Shoulder plates, engraved edges and rivets.
        Ellipse(tex,64,142,54,43,new Color(.20f,.21f,.21f));
        Ellipse(tex,192,142,54,43,new Color(.20f,.21f,.21f));
        Ellipse(tex,64,151,45,31,kind==2?new Color(.12f,.24f,.14f):accent*.42f);
        Ellipse(tex,192,151,45,31,kind==2?new Color(.12f,.24f,.14f):accent*.42f);
        Line(tex,40,154,83,132,3,new Color(.62f,.48f,.29f));
        Line(tex,173,132,216,154,3,new Color(.62f,.48f,.29f));
        for(int i=0;i<4;i++){Ellipse(tex,48+i*10,145-i%2*4,3,3,new Color(.85f,.67f,.37f));Ellipse(tex,174+i*10,145-i%2*4,3,3,new Color(.85f,.67f,.37f));}
        // Neck and face with cheek shadows.
        Rect(tex,108,158,40,35,new Color(.31f,.19f,.14f));
        Ellipse(tex,128,213,47,58,new Color(.49f,.32f,.23f));
        Ellipse(tex,108,205,12,25,new Color(.34f,.21f,.17f));
        Ellipse(tex,148,205,12,25,new Color(.34f,.21f,.17f));
        // Face shadow and angular nose.
        Tri(tex,105,222,151,222,128,177,new Color(.35f,.22f,.18f));
        Line(tex,109,222,123,218,3,new Color(.07f,.055f,.05f));
        Line(tex,133,218,147,222,3,new Color(.07f,.055f,.05f));
        Ellipse(tex,115,222,4,4,kind==1?new Color(.25f,.72f,1f):accent);
        Ellipse(tex,141,222,4,4,kind==1?new Color(.25f,.72f,1f):accent);
        Line(tex,128,213,124,198,2,new Color(.25f,.15f,.13f));
        Line(tex,119,185,137,185,2,new Color(.14f,.07f,.07f));
        // Class-specific headgear.
        if(kind==0)
        {
            Tri(tex,79,228,177,228,128,304,new Color(.28f,.29f,.29f));
            Rect(tex,84,238,88,12,new Color(.49f,.36f,.22f));
            Tri(tex,76,242,100,244,73,270,new Color(.35f,.37f,.37f));
            Tri(tex,180,242,156,244,183,270,new Color(.35f,.37f,.37f));
            Line(tex,128,260,128,293,5,accent);
        }
        else if(kind==1 || kind==3)
        {
            Tri(tex,70,205,186,205,128,315,kind==1?new Color(.035f,.10f,.17f):new Color(.09f,.055f,.12f));
            Tri(tex,87,235,169,235,128,285,kind==1?new Color(.055f,.16f,.24f):new Color(.13f,.075f,.17f));
            if(kind==3){Line(tex,128,266,128,299,4,new Color(.72f,.67f,.53f));Ellipse(tex,128,257,9,9,accent);}
        }
        else
        {
            Tri(tex,79,226,177,226,128,286,new Color(.12f,.20f,.12f));
            Line(tex,87,246,169,246,4,new Color(.43f,.37f,.23f));
            for(int i=0;i<5;i++) Line(tex,154+i*5,262,173+i*5,291,2,new Color(.7f,.57f,.35f));
        }
        // Breastplate, straps, central emblem and warm edge-light.
        Tri(tex,71,125,185,125,128,190,new Color(.20f,.22f,.23f));
        Line(tex,89,156,167,103,7,new Color(.12f,.075f,.055f));
        Line(tex,167,156,89,103,5,new Color(.39f,.29f,.18f));
        Ellipse(tex,128,126,15,18,accent*.65f);
        Ellipse(tex,128,126,6,8,new Color(1f,.64f,.24f));
        Line(tex,42,80,72,102,2,accent*.8f);
        Line(tex,214,80,184,102,2,accent*.8f);
        // A few sparks and etched runes in the portrait backdrop.
        for(int i=0;i<44;i++)
        {
            int x=(i*73+kind*29)%w, y=(i*47+kind*61)%h;
            if(i%3==0) Rect(tex,x,y,2+(i%3),2+(i%4),accent*.75f);
        }
        // Ornate metallic frame and lower vignette.
        Rect(tex,0,0,w,5,new Color(.58f,.39f,.20f));
        Rect(tex,0,h-5,w,5,new Color(.58f,.39f,.20f));
        Rect(tex,0,0,5,h,new Color(.58f,.39f,.20f));
        Rect(tex,w-5,0,5,h,new Color(.58f,.39f,.20f));
        for(int y=0;y<52;y++) for(int x=5;x<w-5;x++)
        {
            Color c=tex.GetPixel(x,y);
            tex.SetPixel(x,y,Color.Lerp(c,new Color(.015f,.018f,.02f),.72f*(1-y/52f)));
        }
        tex.Apply(false,false);
        cached[kind] = Sprite.Create(tex,new Rect(0,0,w,h),new Vector2(.5f,.5f),100);
        return cached[kind];
    }

    static void Rect(Texture2D t,int x,int y,int w,int h,Color c)
    {
        int x0=Mathf.Clamp(x,0,t.width), x1=Mathf.Clamp(x+w,0,t.width);
        int y0=Mathf.Clamp(y,0,t.height), y1=Mathf.Clamp(y+h,0,t.height);
        for(int py=y0;py<y1;py++) for(int px=x0;px<x1;px++) t.SetPixel(px,py,c);
    }
    static void Ellipse(Texture2D t,int cx,int cy,int rx,int ry,Color c)
    {
        rx=Mathf.Max(1,rx); ry=Mathf.Max(1,ry);
        for(int y=Mathf.Max(0,cy-ry);y<Mathf.Min(t.height,cy+ry);y++)
        for(int x=Mathf.Max(0,cx-rx);x<Mathf.Min(t.width,cx+rx);x++)
        {
            float dx=(x-cx)/(float)rx,dy=(y-cy)/(float)ry;
            if(dx*dx+dy*dy<=1f)t.SetPixel(x,y,c);
        }
    }
    static void Line(Texture2D t,int x0,int y0,int x1,int y1,int width,Color c)
    {
        int steps=Mathf.Max(Mathf.Abs(x1-x0),Mathf.Abs(y1-y0));
        for(int i=0;i<=steps;i++)
        {
            float k=steps==0?0:i/(float)steps;
            Ellipse(t,Mathf.RoundToInt(Mathf.Lerp(x0,x1,k)),Mathf.RoundToInt(Mathf.Lerp(y0,y1,k)),width,width,c);
        }
    }
    static void Tri(Texture2D t,int x1,int y1,int x2,int y2,int x3,int y3,Color c)
    {
        int minX=Mathf.Max(0,Mathf.Min(x1,Mathf.Min(x2,x3))), maxX=Mathf.Min(t.width-1,Mathf.Max(x1,Mathf.Max(x2,x3)));
        int minY=Mathf.Max(0,Mathf.Min(y1,Mathf.Min(y2,y3))), maxY=Mathf.Min(t.height-1,Mathf.Max(y1,Mathf.Max(y2,y3)));
        float den=(y2-y3)*(x1-x3)+(x3-x2)*(y1-y3);
        if(Mathf.Abs(den)<.001f)return;
        for(int y=minY;y<=maxY;y++)for(int x=minX;x<=maxX;x++)
        {
            float a=((y2-y3)*(x-x3)+(x3-x2)*(y-y3))/den;
            float b=((y3-y1)*(x-x3)+(x1-x3)*(y-y3))/den;
            float d=1-a-b;
            if(a>=0&&b>=0&&d>=0)t.SetPixel(x,y,c);
        }
    }
}