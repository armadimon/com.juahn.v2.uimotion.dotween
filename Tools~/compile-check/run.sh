#!/usr/bin/env bash
#
# DOTween 백엔드 컴파일 게이트.
#
#   ./Tools~/compile-check/run.sh
#   DOTWEEN_DLL=/path/to/DOTween.dll ./Tools~/compile-check/run.sh
#
# DOTween은 에셋스토어 유료 에셋이라 이 저장소에 넣을 수 없다. 형제 Unity
# 프로젝트에서 찾거나 DOTWEEN_DLL로 직접 지정한다.
#
set -euo pipefail

HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PACKAGE_ROOT="$(cd "${HERE}/../.." && pwd)"

# --- 런타임 패키지 ------------------------------------------------------
if [ -z "${RUNTIME_PACKAGE:-}" ]; then
  RUNTIME_PACKAGE="$(cd "${PACKAGE_ROOT}/../com.juahn.v2.uimotion" 2>/dev/null && pwd || true)"
fi

if [ -z "${RUNTIME_PACKAGE}" ] || [ ! -d "${RUNTIME_PACKAGE}/Runtime/Core" ]; then
  echo "런타임 패키지를 찾지 못했습니다. RUNTIME_PACKAGE를 지정하세요." >&2
  echo "  예: RUNTIME_PACKAGE=/path/to/com.juahn.v2.uimotion $0" >&2
  exit 2
fi

# --- DOTween.dll --------------------------------------------------------
if [ -z "${DOTWEEN_DLL:-}" ]; then
  DOTWEEN_DLL="$(ls "${PACKAGE_ROOT}"/../../*/Assets/Plugins/Demigiant/DOTween/DOTween.dll 2>/dev/null | head -1 || true)"
fi

if [ -z "${DOTWEEN_DLL}" ] || [ ! -f "${DOTWEEN_DLL}" ]; then
  echo "DOTween.dll을 찾지 못했습니다. DOTWEEN_DLL로 지정하세요." >&2
  echo "  예: DOTWEEN_DLL=/path/to/Assets/Plugins/Demigiant/DOTween/DOTween.dll $0" >&2
  exit 2
fi

# --- Unity 설치 ---------------------------------------------------------
if [ -z "${UNITY_ROOT:-}" ]; then
  UNITY_ROOT="$(ls -d /Applications/Unity/Hub/Editor/*/ 2>/dev/null | sort -V | tail -1 || true)"
fi

if [ -z "${UNITY_ROOT}" ] || [ ! -d "${UNITY_ROOT}" ]; then
  echo "Unity 설치를 찾지 못했습니다. UNITY_ROOT를 지정하세요." >&2
  exit 2
fi

UNITY_MANAGED="${UNITY_ROOT}/Unity.app/Contents/Resources/Scripting/Managed/UnityEngine"
if [ ! -d "${UNITY_MANAGED}" ]; then
  echo "Unity 매니지드 폴더를 찾지 못했습니다: ${UNITY_MANAGED}" >&2
  echo "이 스크립트는 macOS 레이아웃을 가정합니다." >&2
  exit 2
fi

if [ -z "${UNITY_UGUI:-}" ]; then
  UNITY_UGUI="$(ls "${UNITY_ROOT}"/Unity.app/Contents/Resources/PackageManager/ProjectTemplates/libcache/*/ScriptAssemblies/UnityEngine.UI.dll 2>/dev/null | head -1 || true)"
fi

if [ -z "${UNITY_UGUI}" ] || [ ! -f "${UNITY_UGUI}" ]; then
  echo "UnityEngine.UI.dll을 찾지 못했습니다. UNITY_UGUI로 지정하세요." >&2
  exit 2
fi

echo "Unity:    ${UNITY_ROOT}"
echo "런타임:   ${RUNTIME_PACKAGE}"
echo "DOTween:  ${DOTWEEN_DLL}"
echo

dotnet build "${HERE}/UiMotion.DoTween.Compile.csproj" \
  -p:UnityManaged="${UNITY_MANAGED}" \
  -p:UnityUgui="${UNITY_UGUI}" \
  -p:RuntimePackage="${RUNTIME_PACKAGE}" \
  -p:DoTweenDll="${DOTWEEN_DLL}" \
  -v quiet --nologo

echo
echo "DOTween 백엔드 컴파일 통과."
