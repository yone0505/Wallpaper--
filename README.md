# Wallpaper--

Windows 动态壁纸软件重建项目。目标是对照本机 Wallpaper Engine 2.8.42 的功能、界面和实际行为，而不是仅制作可导入的壁纸。

目前完成了原版资源调查、部分运行界面采集和验收范围记录，尚未交付可独立运行的重建软件。

- [原版版本与界面对照基线](docs/reference/wallpaper-engine-2.8.42.md)
- [61 项功能与界面验收记录](docs/reference/parity-baseline.json)
- [开发与 GitHub 同步规范](AGENTS.md)

## 当前验证命令

在 Windows PowerShell 中运行：

```powershell
./tests/Test-RepositoryPolicy.ps1
./tests/Test-IgnoredArtifacts.ps1
./tests/Test-ParityBaseline.ps1
./tests/Test-ParityBaselineRegression.ps1
```

这些测试验证项目规范与验收记录结构。实际功能和界面的一致性须由各项目的运行、视觉和外部集成测试另行证明；当前尚无项目通过产品一致性验收。
