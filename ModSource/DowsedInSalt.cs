using System;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.API.Client;
using Vintagestory.API.Server;

public class DowsedInSalt : ModSystem, IDisposable {
    private ICoreClientAPI api;
    private ILoadedSound sound;
    private float accum = 0.0f;
    private const int NUM_SAMPLES = 500;
    private const int LOUDEST_SAMPLES = 20;
    private const int SAMPLE_RADIUS = 10;
    private AssetLocation halite;
    public override void StartClientSide(ICoreClientAPI api) {
        base.StartClientSide(api);
        this.api = api;
        api.Event.RegisterGameTickListener(OnGameTick, 100);
        halite = new AssetLocation("game", "rock-halite");
    }
    private void OnGameTick(float deltaTime) {
        // freak out if time goes backwards
        if(deltaTime < 0.0f) {
            accum = 0.0f;
            return;
        }
        // go once per second, ish
        accum += deltaTime;
        if(accum >= 1.0f) {
            if(accum <= 2.0f) {
                accum -= 1.0f;
            }
            else {
                // this will also happen if we get a NaN delta-time
                accum = 0.0f;
            }
        }
        else {
            return;
        }
        float volume = check_volume();
        if(volume > 0.0 && sound == null) {
            SoundParams param = new SoundParams(new AssetLocation("dowsedinsalt", "sounds/shikkashikka"));
            param.RelativePosition = true;
            param.ShouldLoop = true;
            sound = api.World.LoadSound(param);
        }
        if(sound != null) {
            if(!sound.IsPlaying) {
                sound.SetVolume(0.0f);
                sound.Start();
            }
            if(volume == 0.0f) {
                sound.FadeOutAndStop(1.0f);
                sound = null;
            }
            else {
                sound.FadeTo(volume, 1.0f, null);
            }
        }
    }
    private float check_volume() {
        // go only if there is a player, alive
        var world = api.World;
        if(world == null) return 0.0f;
        var player = world.Player?.Entity;
        if(player == null || !player.Alive) return 0.0f;
        // and drunk
        var wat = player.WatchedAttributes.GetFloat("intoxication");
        // find salt nearby
        int count = 0;
        if(wat > 0.0f) {
            Block block = world.GetBlock(halite);
            if(block == null) {
                api.ShowChatMessage("no such thing as halite??? ("+halite.ToString()+")");
                return 0.0f;
            }
            int bid = block.Id;
            Random rng = new Random();
            int px = (int)player.Pos.X;
            int py = (int)player.Pos.Y;
            int pz = (int)player.Pos.Z;
            for(int n = 0; n < NUM_SAMPLES; ++n) {
                if(sample_salt(world, rng, px, py, pz, bid)) {
                    count += 1;
                }
            }
        }
        // shikka shikka
        return Math.Min(((float)count) / ((float)LOUDEST_SAMPLES), 1.0f) * Math.Min(wat * 10.0f, 1.0f);
    }
    private bool sample_salt(IClientWorldAccessor world, Random rng, int px, int py, int pz, int bid) {
        int xoff = rng.Next(SAMPLE_RADIUS*2+1)-SAMPLE_RADIUS;
        int zoff = rng.Next(SAMPLE_RADIUS*2+1)-SAMPLE_RADIUS;
        int yoff = -rng.Next(SAMPLE_RADIUS);
        Block block = world.BlockAccessor.GetBlock(px + xoff, py + yoff, pz + zoff);
        return block.Id == bid;
    }
    public override void Dispose() {
        base.Dispose();
        if(sound != null) {
            sound.Dispose();
            sound = null;
        }
        api = null;
    }
}
