# account-server Dockerfile — 多模块构建
# Build context: Server/ (父目录)

# ─── 构建阶段 ───
FROM maven:3.9-eclipse-temurin-21 AS builder

WORKDIR /build
# 复制所有 pom（Maven reactor 需要全部模块 pom 才能解析）
COPY pom.xml .
COPY network/pom.xml network/pom.xml
COPY game-server/pom.xml game-server/pom.xml
COPY account-server/pom.xml account-server/pom.xml
# 预取依赖
RUN mvn -pl account-server -am dependency:go-offline -B || true

# 复制源码并构建
COPY account-server/src account-server/src
RUN mvn -pl account-server -am clean package -DskipTests -B

# ─── 运行阶段 ───
FROM eclipse-temurin:21-jre-jammy

WORKDIR /app
COPY --from=builder /build/account-server/target/*.jar app.jar

EXPOSE 8081
ENTRYPOINT ["java", "-jar", "app.jar"]
