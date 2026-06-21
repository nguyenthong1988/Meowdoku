using System;
using System.Collections.Generic;
using CaskFramework.Assets;
using CaskFramework.Assets.Pool;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Cast.Game.Board
{

    public sealed class CellViewPool
    {
        
        private const int MaxBoard = 15;
        
        private const int WarmBoard = 9;

        private readonly IAssetManager _assets;
        private readonly string _cellAddress;
        private readonly string _tag;
        private ObjectPool _pool;

        public bool IsReady => _pool != null;

        public CellViewPool(IAssetManager assets, string cellAddress, string tag = "meowdoku.cells")
        {
            _assets = assets ?? throw new ArgumentNullException(nameof(assets));
            _cellAddress = cellAddress;
            _tag = tag;
        }

        public async UniTask PreloadAsync(GameObject parent)
        {
            if (_pool != null) return;

            if (_assets.IsObjectPoolCreated(_tag))
            {
                _pool = _assets.GetObjectPool(_tag);
                return;
            }

            GameObject prefab = await _assets.PreloadAsync<GameObject>(_cellAddress, _tag);
            _pool = await _assets.CreatePoolAsync(
                prefab, _tag, parent,
                initSize: WarmBoard * WarmBoard,    
                capacity: MaxBoard * MaxBoard);     
        }

        public List<CellView> Take(int count, Transform parent) =>
            _pool != null ? _pool.TakeBatch<CellView>(count, active: true, parent: parent) : null;

        public void ReturnAll() => _pool?.ReturnAll();

        public void Release()
        {
            _pool?.Release();
            _pool = null;
            _assets.WeakRelease(_tag);
        }
    }
}
