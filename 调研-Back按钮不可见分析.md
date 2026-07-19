# Collections Back Button 不可见 — 根因分析与方案

## 对比对象

| | Collections 按钮 (LevelSelectMenu2) | Back 按钮 (CollectionsMenu) |
|---|---|---|
| 状态 | ✅ 正常显示、可交互 | ❌ 不可见 |
| 代码位置 | `Plugin.cs` `InjectCollectionsButton()` | `CollectionsMenu.cs` `BuildBackButton()` |

---

## 核心差异

### 1. 父对象不同 → CanvasGroup 继承完全不同

```
✅ Collections 按钮的层级:
  LevelSelectMenu2 (MenuTransition, CanvasGroup.alpha=1)
    └── topPanel (AutoNavigation, 无 CanvasGroup)
          └── CollectionsTitle (按钮) ← 继承 alpha=1

❌ Back 按钮的层级:
  CollectionsMenu (MenuTransition, CanvasGroup 受 Apply() 操控)
    └── CollectionsBackBtn (按钮) ← 继承父 CanvasGroup alpha
```

**关键**：`MenuTransition.Apply()` 会设置自身的 `CanvasGroup.alpha = 1 - |current|`。而 Unity CanvasGroup.alpha 会**递归影响所有子对象**。

### 2. 创建时机不同 → 过渡动画影响不同

```
✅ Collections 按钮:
  创建于 Bootstrapper 协程中
  LevelSelectMenu2 此时已稳定 active，CanvasGroup.alpha = 1
  Instantiate → SetActive(true) → 立即可见

❌ Back 按钮:
  创建于 FadeInForward 触发的 OnEnable 中:
    ① SetActive(true) → OnEnable → BuildOnce → BuildBackButton → SetActive(true)
                                                        ↑ 按钮此时 alpha=1
    ② Transition(1f, 0f) → Apply() → CanvasGroup.alpha = 0
                                                        ↑ 按钮立即不可见！
    ③ Transition(0f, 0.3f) → Update() 驱动 alpha: 0 → 1
                                                        ↑ 0.3 秒后才可见
```

如果动画正常运行，按钮应在 0.3 秒后可见。**如果始终不可见，说明动画没有正常完成，或者有第二层原因。**

### 3. RectTransform 策略不同

| | Collections 按钮 | Back 按钮 |
|---|---|---|
| anchor/pivot | 保留模板原始值 | 完全覆盖为 (0,0) |
| 位置 | 微调 anchoredPosition += (-8, -8) | 设为 (40, 40) |
| sibling | SetAsFirstSibling (标签栏最左) | SetAsLastSibling (CollectionsMenu 末尾) |

---

## 可能根因（按可能性排序）

### ★ 假设 1：过渡动画未完成，按钮始终继承 alpha=0

`FadeInForward` 中的时序：
```csharp
to.gameObject.SetActive(true);    // OnEnable → BuildOnce → 按钮 SetActive(true)
to.Transition(1f, 0f);            // CanvasGroup.alpha = 0 ← 按钮立即被隐藏
to.Transition(0f, fadeInTime);    // 启动 Update 动画
to.OnGotFocus();                  // 此时 current 还不等于 0，alpha < 1
```

`Transition(1f, 0f)` 中 duration=0，**立即**将 `current=1, phase=1, target=1` 并调用 `Apply()`。

然后 `Transition(0f, 0.3f)` 设置 `target=0, phase=0, start=1`。

`Update()` 每帧逐步推进 phase → 1，`current` 从 1 缓动到 0，alpha 从 0 到 1。

**可验证点**：如果过渡开始但 `current` 卡在中间值（比如 `Update()` 没在执行），按钮就会保持不可见/半透明。可能原因：
- GameObject 在过渡中被意外 deactivate（`Update` 在 inactive GameObject 上不执行）
- `MenuSystem.OnEnable()` 被反复触发，每次都把 CollectionsMenu 设为 inactive

### ★ 假设 2：模板 BackButton 引用了外部对象

`FindTemplateButton()` 返回 `lsm2.BackButton`。这个 BackButton 可能在 prefab 中被序列化引用了 LevelSelectMenu2 层级外的对象（如 MenuSystem 上的某个组件）。当 `Instantiate` 克隆时：
- 引用**同层级内**的对象 → 正确重映射
- 引用**外部**对象 → 保持指向原始对象，可能访问已失效的数据

### ★ 假设 3：MenuButton.Awake() 在克隆时被触发，状态异常

`Instantiate` 会触发所有组件的 `Awake()`。`MenuButton.Awake()` 查找 `label` 引用。如果在模板中 `label` 已经通过序列化设置，则不需要重新查找。但如果序列化引用指向了错误的对象，可能导致 `DoStateTransition` 中 CrossFadeColor 作用在错误的 Graphic 上。

### ★ 假设 4：FindTemplateButton 返回 null，兜底按钮仍有无 sprite 问题

虽然已添加 `GetDefaultSprite()`，但在某些 Unity 版本中，`Texture2D.Apply()` 后立即创建 `Sprite.Create` 可能因为纹理未上传到 GPU 而失败。需要确保纹理是 `isReadable` 且正确应用。

### ★ 假设 5：Canvas 渲染排序

两个 MenuTransition (LevelSelectMenu2 淡出中、CollectionsMenu 淡入中) 的 `localPosition.z` 分别为负值和正值，导致渲染顺序变化。过渡完成后都回到 z=0。但如果 CollectionsMenu 的 sibling index 使得它排在所有 pagePrefabs 之后，而 pagePrefabs 中某个全屏不透明的 UI 元素覆盖了它...

---

## 建议验证方案

### 方案 A：在 BuildBackButton 末尾强制设置父 CanvasGroup alpha

```csharp
// 在 go.SetActive(true) 之后立即添加
var parentCG = GetComponent<CanvasGroup>();
Plugin.Logger.LogInfo($"BackBtn activeSelf={go.activeSelf}, " +
    $"activeInHierarchy={go.activeInHierarchy}, " +
    $"parentCG.alpha={parentCG?.alpha}");
```

如果 parentCG.alpha ≠ 1，说明过渡动画没完成或卡住了。

### 方案 B：延迟创建 Back 按钮

不在 `OnEnable()` 中创建，而是在 `OnGotFocus()` 中创建（此时过渡动画已完成，当前菜单的 alpha 已经为 1）：

```csharp
// 把 BuildBackButton 移入 OnGotFocus 或在 OnGotFocus 末尾调用
```

### 方案 C：不完全依赖模板 — 强制走 from-scratch 路径

```csharp
// BuildBackButton 中传入 null 作为 template
var go = CloneOrCreateButton(null, "CollectionsBackBtn");
```

彻底绕过模板相关的任何未知状态。

### 方案 D：使用跟 Collections 按钮完全一样的模式

Collections 按钮之所以能工作，核心是：
1. 父对象是 `topPanel`（无 CanvasGroup）
2. 不修改 anchor/pivot
3. 创建时父菜单已稳定 active

对 Back 按钮也可以尝试：把 Back 按钮放在一个独立的不受 MenuTransition 影响的容器下，或者挂在 MenuSystem 的某个稳定容器上。

---

## 推荐的修复顺序

1. **先加日志**（方案 A），确认运行时状态
2. **如果 parentCG.alpha ≠ 1** → 方案 B（延迟创建到 OnGotFocus）
3. **如果 parentCG.alpha = 1 但仍然不可见** → 方案 C（强制 from-scratch）
4. **还不行** → 方案 D（调整父对象层级，避开 CanvasGroup 继承）
