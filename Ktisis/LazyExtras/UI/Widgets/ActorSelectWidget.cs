﻿using Dalamud.Interface;
using Dalamud.Interface.Utility.Raii;

using GLib.Widgets;

using ImGuiNET;

using Ktisis.Common.Extensions;
using Ktisis.Common.Utility;
using Ktisis.Editor.Context.Types;
using Ktisis.Interface.Components.Workspace;
using Ktisis.LazyExtras.Interfaces;
using Ktisis.Scene.Decor;
using Ktisis.Scene.Entities;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace Ktisis.LazyExtras.UI.Widgets;
class ActorSelectWidget :ILazyWidget {
	private readonly IEditorContext _ctx;
	private readonly SceneDragDropHandler _dragDrop;

	// LazyWidget
	public LazyWidgetCat Category { get; }
	public int CustomOrder { get; set; }
	public bool InToolbelt { get; set; }
	public bool SupportsToolbelt { get; }
	public Vector2 SizeToolbelt { get; }

	public ActorSelectWidget(IEditorContext ctx) {

		this._ctx = ctx;
		this._dragDrop = new SceneDragDropHandler(ctx);

		// LazyWidget
		this.Category = LazyWidgetCat.Selection;
		this.InToolbelt = false;
		this.SupportsToolbelt = false;
		this.SizeToolbelt = Vector2.Zero;
	}
	public void Draw() {
		this.Draw(400.0f);
	}

	// For testing purposes

	// Draw frame

	public void Draw(float height) {
		var frame = false;
		try {
			var id = ImGui.GetID("SceneTree_Frame");
			frame = ImGui.BeginChildFrame(id, new Vector2(-1, height));
			if (!frame) return;
			this.DrawScene(height);
		} catch (Exception err) {
			Ktisis.Log.Error($"Error drawing scene tree:\n{err}");
		} finally {
			if (frame) ImGui.EndChildFrame();
		}
	}

	// Draw scene entities

	private static float IconSpacing => UiBuilder.IconFont.FontSize;


	private float MinY;
	private float MaxY;

	private void PreCalc(float height) {
		var scroll = ImGui.GetScrollY();
		this.MinY = scroll - ImGui.GetFrameHeight();
		this.MaxY = height + scroll;
	}

	private void DrawScene(float height) {
		this.PreCalc(height);

		var spacing = ImGui.GetStyle().ItemSpacing;
		using var _ = ImRaii.PushStyle(ImGuiStyleVar.ItemSpacing, spacing with { Y = 5.0f });
		this.IterateTree(this._ctx.Scene.Children);
	}

	private void IterateTree(IEnumerable<SceneEntity> entities) {
		try {
			ImGui.TreePush();
			foreach (var item in entities)
				this.DrawNode(item);
		} finally {
			ImGui.TreePop();
		}
	}

	// Nodes

	private enum TreeNodeFlag {
		Leaf,
		Expand,
		Collapse
	}

	private void DrawNode(SceneEntity node) {
		var pos = ImGui.GetCursorPos();
		var isRender = pos.Y > this.MinY && pos.Y < this.MaxY;

		var id = $"##SceneTree_{node.GetHashCode():X}";

		const ImGuiSelectableFlags selectFlags = ImGuiSelectableFlags.AllowItemOverlap | ImGuiSelectableFlags.SpanAllColumns;
		ImGui.Selectable(id, node.IsSelected, selectFlags);

		var isHover = ImGui.IsWindowHovered();
		var size = ImGui.GetItemRectSize();
		
		// NOTE: This blocks ImGUi.IsWindowHovered for some stupid reason
		this._dragDrop.Handle(node);

		var imKey = ImGui.GetID(id);
		var state = ImGui.GetStateStorage();
		
		var isExpand = state.GetBool(imKey);

		var children = node.Children.ToList();
		
		if (isRender) {
			var flag = isExpand switch {
				_ when children.Count is 0 => TreeNodeFlag.Leaf,
				true => TreeNodeFlag.Expand,
				false => TreeNodeFlag.Collapse
			};

			var rightAdjust = this.DrawButtons(node, isHover);
			if (this.DrawNodeLabel(node, pos, flag, rightAdjust))
				state.SetBool(imKey, isExpand = !isExpand);

			if (isHover && this.IsNodeHovered(pos, size, rightAdjust)) {
				if (ImGui.IsMouseDoubleClicked(ImGuiMouseButton.Left)) {
					this._ctx.Interface.OpenEditorFor(node);
				} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Left)) {
					var mode = GuiHelpers.GetSelectMode();
					node.Select(mode);
				} else if (ImGui.IsMouseClicked(ImGuiMouseButton.Right)) {
					this._ctx.Interface.OpenSceneEntityMenu(node);
				}
			}
		}

		if (isExpand) this.IterateTree(children);
	}

	private bool DrawNodeLabel(SceneEntity item, Vector2 pos, TreeNodeFlag flag, float rightAdjust = 0.0f) {
		var display = this._ctx.Config.GetEntityDisplay(item);
		
		// Caret

		var style = ImGui.GetStyle();
		ImGui.SameLine();
		ImGui.SetCursorPosX(pos.X - style.ItemSpacing.X);
		var expand = this.DrawNodeCaret(display.Color, flag);
		
		// Icon + Label

		using var _ = ImRaii.PushColor(ImGuiCol.Text, display.Color);
		this.DrawNodeIcon(display.Icon);
			
		var avail = ImGui.GetContentRegionAvail().X;
		ImGui.Text(item.Name.FitToWidth(avail - rightAdjust));

		return expand;
	}

	private bool DrawNodeCaret(uint color, TreeNodeFlag flag) {
		var cursor = ImGui.GetCursorPosX();

		var caretIcon = flag switch {
			TreeNodeFlag.Collapse => FontAwesomeIcon.CaretRight,
			TreeNodeFlag.Expand => FontAwesomeIcon.CaretDown,
			_ => FontAwesomeIcon.None
		};
		
		using (var _ = ImRaii.PushColor(ImGuiCol.Text, color.SetAlpha(0xCF)))
			Icons.DrawIcon(caretIcon);

		ImGui.SameLine();
		
		var spacing = ImGui.GetStyle().ItemInnerSpacing;
		cursor += spacing.X + IconSpacing;
		ImGui.SetCursorPosX(cursor);
		
		var iconSize = Icons.CalcIconSize(caretIcon);
		var frameHeight = ImGui.GetFrameHeight();
		return ButtonsEx.IsClicked(
			new Vector2(
				IconSpacing - iconSize.X,
				(frameHeight - iconSize.Y - spacing.Y / 2) / 2
			)
		);
	}

	private void DrawNodeIcon(FontAwesomeIcon icon) {
		var hasIcon = icon != FontAwesomeIcon.None;
		var iconPadding = hasIcon ? Icons.CalcIconSize(icon).X / 2.0f : 0.0f;
		var iconSpace = hasIcon ? IconSpacing : 0;
		Icons.DrawIcon(icon);
		ImGui.SameLine(0, iconSpace - iconPadding);
	}

	// Buttons

	private float DrawButtons(SceneEntity node, bool isHover) {
		var initial = ImGui.GetCursorPosX() + ImGui.GetContentRegionAvail().X;
		var cursor = initial;

		this.DrawVisibilityButton(node, ref cursor, isHover);
		if (node is IAttachable attach)
			this.DrawAttachButton(attach, ref cursor, isHover);
		
		return initial - cursor;
	}

	private void DrawVisibilityButton(SceneEntity node, ref float cursor, bool isHover) {
		if (node is not IVisibility vis) return;

		var isActive = this._ctx.Config.Overlay.Visible;
		
		var isVisible = vis.Visible;
		var color = isVisible ? 0xEFFFFFFF : 0x80FFFFFF;
		if (!isActive)
			color = color.SetAlpha((byte)(isVisible ? 0x60 : 0x30));
		
		if (this.DrawButton(ref cursor, FontAwesomeIcon.Eye, color) && isHover)
			vis.Toggle();
	}

	private void DrawAttachButton(IAttachable attach, ref float cursor, bool isHover) {
		if (!attach.IsAttached()) return;
		
		if (this.DrawButton(ref cursor, FontAwesomeIcon.Link, 0xFFFFFFFF) && isHover)
			this._ctx.Posing.Attachments.Detach(attach);

		if (!isHover || !ImGui.IsItemHovered()) return;

		var bone = attach.GetParentBone();
		var name = bone != null ? this._ctx.Locale.GetBoneName(bone) : "UNKNOWN";
		using var _ = ImRaii.Tooltip();
		ImGui.Text($"Attached to {name}");
	}

	private bool DrawButton(ref float cursor, FontAwesomeIcon icon, uint? color = null) {
		cursor -= Icons.CalcIconSize(icon).X + ImGui.GetStyle().ItemSpacing.X;
		ImGui.SameLine();
		ImGui.SetCursorPosX(cursor);
		using var _ = ImRaii.PushColor(ImGuiCol.Text, color ?? 0, color.HasValue);
		Icons.DrawIcon(icon);
		return ButtonsEx.IsClicked();
	}

	// Handle hover + click

	private bool IsNodeHovered(Vector2 pos, Vector2 size, float rightAdjust) {
		var pad = ImGui.GetStyle().ItemSpacing.X;
		var min = ImGui.GetWindowPos() + pos.AddX(pad).SubY(ImGui.GetScrollY() + 2);
		var max = min.Add(size.X - pos.X - pad - rightAdjust, size.Y);
		return ImGui.IsMouseHoveringRect(min, max);
	}


}
